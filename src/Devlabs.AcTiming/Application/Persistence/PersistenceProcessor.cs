using Devlabs.AcTiming.Application.Abstractions;
using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devlabs.AcTiming.Application.Persistence;

public sealed class PersistenceProcessor(
    ILogger<PersistenceProcessor> logger,
    PersistenceBus persistenceBus,
    IServiceScopeFactory scopeFactory
) : BackgroundService
{
    // Current session — null means no active session. Id and TrackId are used as FK values
    // across scopes (the object itself is detached after each handler).
    private Session? _session;

    // Per-car slot: which driver+car is occupying each AC slot
    private readonly Dictionary<int, CarSlotInfo> _carSlots = new();

    // Telemetry snapshot waiting to be consumed by the next LapCompleted for that car
    private readonly Dictionary<int, SimEventLapSnapshotted> _lapSnapshots = new();

    // Latest CompletedSectorsThisLap per car (overwritten on every SectorCrossed)
    private readonly Dictionary<int, List<int>> _sectorTimes = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PersistenceProcessor started");
        await ConsumeEventsAsync(stoppingToken);
        logger.LogInformation("PersistenceProcessor stopped");
    }

    private async Task ConsumeEventsAsync(CancellationToken ct)
    {
        await foreach (var ev in persistenceBus.Reader.ReadAllAsync(ct))
        {
            try
            {
                switch (ev)
                {
                    case SimEventSessionInfoReceived s:
                        await HandleSessionInfoAsync(s, ct);
                        break;

                    case SimEventSessionEnded:
                        await HandleSessionEndedAsync(ct);
                        break;

                    case SimEventCarInfoReceived c:
                        _carSlots[c.CarId] = new CarSlotInfo(
                            c.DriverGuid,
                            c.DriverName,
                            Team: null,
                            c.CarModel,
                            c.CarSkin
                        );
                        break;

                    case SimEventDriverConnected d:
                        _carSlots[d.CarId] = new CarSlotInfo(
                            d.DriverGuid,
                            d.DriverName,
                            Team: null,
                            d.CarModel,
                            d.CarSkin
                        );
                        break;

                    case SimEventSectorCrossed s:
                        _sectorTimes[s.CarId] = [.. s.CompletedSectorsThisLap];
                        break;

                    case SimEventLapSnapshotted s:
                        _lapSnapshots[s.CarId] = s;
                        break;

                    case SimEventLapCompleted l:
                        await HandleLapCompletedAsync(l, ct);
                        break;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error handling persistence event {EventType}",
                    ev.GetType().Name
                );
            }
        }
    }

    // ─── Session lifecycle ────────────────────────────────────────────────────

    private async Task HandleSessionInfoAsync(SimEventSessionInfoReceived ev, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ITimingDb>();
        var session = await db.AsTracking<Session>()
            .Include(s => s.Track)
            .SingleOrDefaultAsync(s => s.EndedAtUtc == null, ct);

        if (session is not null)
        {
            if (session.ProbablySameSession(ev.TrackName, ev.TrackConfig, ev.Type))
            {
                logger.LogInformation(
                    "Received session info for existing session {SessionId}, resuming",
                    session.Id
                );
                _session = session;
                return;
            }
            session.Abort();
        }

        var track = await db.AsTracking<Track>()
            .FirstOrDefaultAsync(t => t.Name == ev.TrackName && t.Config == ev.TrackConfig, ct);
        if (track is null)
        {
            logger.LogInformation(
                "New track detected: {TrackName} ({TrackConfig})",
                ev.TrackName,
                ev.TrackConfig
            );
            track = new Track { Name = ev.TrackName, Config = ev.TrackConfig };
            db.Insert(track);
        }

        var newSession = Session.Open(
            track,
            ev.ServerName,
            ev.Type,
            ev.Time,
            ev.Laps,
            (int)ev.AmbientTemp,
            (int)ev.RoadTemp
        );
        db.Insert(newSession);
        await db.SaveChangesAsync(ct);

        _session = newSession;
        ResetPerSessionState();
    }

    private async Task HandleSessionEndedAsync(CancellationToken ct)
    {
        if (_session is null)
            return;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ITimingDb>();

        var session = await db.AsTracking<Session>()
            .SingleOrDefaultAsync(s => s.Id == _session.Id, ct);

        if (session is null)
            return;

        session.Close(SessionClosedReason.Natural);
        await db.SaveChangesAsync(ct);
        _session = null;
    }

    // ─── Lap recording ────────────────────────────────────────────────────────

    private async Task HandleLapCompletedAsync(SimEventLapCompleted ev, CancellationToken ct)
    {
        if (_session is null)
        {
            logger.LogWarning(
                "Lap completed for car {CarId} but no active session — skipping",
                ev.CarId
            );
            return;
        }

        if (!_carSlots.TryGetValue(ev.CarId, out var slot))
        {
            logger.LogWarning(
                "Lap completed for car {CarId} but no slot info — skipping",
                ev.CarId
            );
            return;
        }

        _lapSnapshots.Remove(ev.CarId, out var snapshot);
        _sectorTimes.Remove(ev.CarId, out var sectors);

        var lapNumber = ev.Leaderboard.FirstOrDefault(e => e.CarId == ev.CarId)?.TotalLaps ?? 0;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ITimingDb>();

        // Upsert driver — name/team can change across sessions
        var driver = await db.AsTracking<Driver>()
            .FirstOrDefaultAsync(d => d.Guid == slot.DriverGuid, ct);
        if (driver is null)
        {
            driver = new Driver
            {
                Guid = slot.DriverGuid,
                Name = slot.DriverName,
                Team = slot.Team,
            };
            db.Insert(driver);
        }
        else
        {
            driver.Name = slot.DriverName;
            driver.Team = slot.Team;
        }

        // Upsert car
        var car = await db.AsTracking<Car>()
            .FirstOrDefaultAsync(c => c.Model == slot.CarModel && c.Skin == slot.CarSkin, ct);
        if (car is null)
        {
            car = new Car { Model = slot.CarModel, Skin = slot.CarSkin };
            db.Insert(car);
        }

        var lap = new Lap
        {
            SessionId = _session.Id,
            TrackId = _session.TrackId,
            Driver = driver,
            Car = car,
            LapNumber = lapNumber,
            LapTimeMs = ev.LapTimeMs,
            SectorTimesMs = sectors ?? [],
            Cuts = ev.Cuts,
            IsValid = ev.Cuts == 0,
            MaxSpeedKmh = snapshot?.MaxSpeedKmh ?? 0f,
            TelemetrySamples = snapshot?.Samples.ToList() ?? [],
            RecordedAtUtc = DateTime.UtcNow,
        };
        db.Insert(lap);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Persisted lap {LapNumber} ({LapTime}ms) for {Driver} in session {SessionId}",
            lapNumber,
            ev.LapTimeMs,
            slot.DriverName,
            _session.Id
        );
    }

    private void ResetPerSessionState()
    {
        _carSlots.Clear();
        _lapSnapshots.Clear();
        _sectorTimes.Clear();
    }
}
