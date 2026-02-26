using System.Collections.Concurrent;
using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Application.LiveTiming;

public sealed class LiveTimingSession
{
    private const int MaxSamplesPerLap = 2000;
    private const float MinSplineStep = 0.002f;
    private const int MinSamplesForValidLap = 20;

    private readonly ConcurrentDictionary<int, LiveDriver> _drivers = new();
    private readonly Lock _feedLock = new();
    private readonly List<SessionFeedEvent> _feedEvents = [];

    // Keyed by CarId — only written from the sequential event pipeline.
    private readonly Dictionary<int, List<LapTelemetrySample>> _currentLapBuffers = new();

    // Keyed by (DriverGuid, CarModel) — read from Blazor UI threads, so concurrent.
    private readonly ConcurrentDictionary<
        (string DriverGuid, string CarModel),
        BestLapTelemetry
    > _bestLaps = new();

    public SessionInfo Info { get; }

    public LiveTimingSession(SimEventSessionInfoReceived ev)
    {
        Info = new SessionInfo(
            ev.ServerName,
            ev.TrackName,
            string.IsNullOrEmpty(ev.TrackConfig) ? null : ev.TrackConfig,
            ev.Type,
            ev.Time,
            ev.Laps,
            ev.ElapsedMs,
            ev.AmbientTemp,
            ev.RoadTemp
        );
    }

    public void Apply(SimEvent ev)
    {
        switch (ev)
        {
            case SimEventDriverConnected d:
                HandleDriverConnected(d);
                break;
            case SimEventCarInfoReceived c:
                HandleCarInfo(c);
                break;
            case SimEventTelemetryUpdated t:
                HandleTelemetry(t);
                break;
            case SimEventSectorCrossed s:
                HandleSectorCrossed(s);
                break;
            case SimEventLapCompleted l:
                HandleLapCompleted(l);
                break;
            case SimEventPitStatusChanged p:
                HandlePitStatus(p);
                break;
            case SimEventCollisionDetected c:
                HandleCollision(c);
                break;
            case SimEventDriverDisconnected d:
                HandleDisconnect(d);
                break;
            case SimEventSpeedTrapFired s:
                HandleSpeedTrapFired(s);
                break;
        }
    }

    public LiveDriver? GetDriver(int carId) => _drivers.GetValueOrDefault(carId);

    public IReadOnlyList<LiveDriver> GetLeaderboard() =>
        _drivers.Values.Where(d => d.IsConnected).OrderBy(d => d.Position).ToList();

    public IReadOnlyList<SessionFeedEvent> GetFeedEvents()
    {
        lock (_feedLock)
            return [.. _feedEvents];
    }

    public IReadOnlyList<BestLapTelemetry> GetBestLaps() => [.. _bestLaps.Values];

    private void HandleDriverConnected(SimEventDriverConnected ev)
    {
        var driver = new LiveDriver
        {
            CarId = ev.CarId,
            DriverName = ev.DriverName,
            DriverGuid = ev.DriverGuid,
            CarModel = ev.CarModel,
            CarSkin = ev.CarSkin,
            IsConnected = true,
        };
        _drivers[ev.CarId] = driver;
        _currentLapBuffers[ev.CarId] = [];
        lock (_feedLock)
            _feedEvents.Add(new DriverJoinedFeed(DateTime.UtcNow, ev.CarId, ev.DriverName));
    }

    private void HandleCarInfo(SimEventCarInfoReceived ev)
    {
        _drivers.AddOrUpdate(
            ev.CarId,
            _ => new LiveDriver
            {
                CarId = ev.CarId,
                DriverName = ev.DriverName,
                DriverGuid = ev.DriverGuid,
                CarModel = ev.CarModel,
                CarSkin = ev.CarSkin,
                IsConnected = true,
            },
            (_, existing) =>
                existing with
                {
                    DriverName = ev.DriverName,
                    DriverGuid = !string.IsNullOrEmpty(ev.DriverGuid)
                        ? ev.DriverGuid
                        : existing.DriverGuid,
                    CarModel = !string.IsNullOrEmpty(ev.CarModel) ? ev.CarModel : existing.CarModel,
                    CarSkin = ev.CarSkin ?? existing.CarSkin,
                }
        );
    }

    private void HandleTelemetry(SimEventTelemetryUpdated ev)
    {
        if (!_drivers.TryGetValue(ev.CarId, out var existing))
            return;

        var updated = existing with
        {
            SplinePosition = ev.SplinePosition,
            WorldX = ev.WorldX,
            WorldZ = ev.WorldZ,
            SpeedKmh = ev.SpeedKmh,
            Gear = ev.Gear,
            EngineRpm = ev.EngineRpm,
            IsConnected = true,
        };
        _drivers.TryUpdate(ev.CarId, updated, existing);

        AppendTelemetrySample(ev);
    }

    private void AppendTelemetrySample(SimEventTelemetryUpdated ev)
    {
        if (!_currentLapBuffers.TryGetValue(ev.CarId, out var buffer))
        {
            buffer = [];
            _currentLapBuffers[ev.CarId] = buffer;
        }

        if (buffer.Count >= MaxSamplesPerLap)
            return;

        // Skip if spline position hasn't advanced enough (also handles AFK: stationary car adds 1 sample then stops).
        if (
            buffer.Count > 0
            && Math.Abs(ev.SplinePosition - buffer[^1].SplinePosition) < MinSplineStep
        )
            return;

        buffer.Add(
            new LapTelemetrySample(ev.SplinePosition, ev.WorldX, ev.WorldZ, ev.SpeedKmh, ev.Gear)
        );
    }

    private void HandleSectorCrossed(SimEventSectorCrossed ev)
    {
        if (!_drivers.TryGetValue(ev.CarId, out var existing))
            return;

        var bestSectors = existing.BestSectorTimesMs;
        if (ev.IsValidLap && ev.SectorIndex == 2 && ev.CompletedSectorsThisLap.Count == 3)
            bestSectors = UpdateBestSectors(existing.BestSectorTimesMs, ev.CompletedSectorsThisLap);

        var updated = existing with
        {
            LastSectorTimesMs = ev.CompletedSectorsThisLap,
            BestSectorTimesMs = bestSectors,
        };
        _drivers.TryUpdate(ev.CarId, updated, existing);
    }

    private void HandleLapCompleted(SimEventLapCompleted ev)
    {
        if (_drivers.TryGetValue(ev.CarId, out var driver))
        {
            var isNewBest =
                ev.Cuts == 0
                && (driver.BestLapTimeMs is null || ev.LapTimeMs < driver.BestLapTimeMs);

            var bestLap = isNewBest ? ev.LapTimeMs : driver.BestLapTimeMs;

            if (
                isNewBest
                && _currentLapBuffers.TryGetValue(ev.CarId, out var buffer)
                && buffer.Count >= MinSamplesForValidLap
            )
            {
                var key = (driver.DriverGuid, driver.CarModel);
                _bestLaps[key] = new BestLapTelemetry(
                    driver.DriverGuid,
                    driver.DriverName,
                    driver.CarModel,
                    ev.LapTimeMs,
                    buffer.ToList()
                );
            }

            _currentLapBuffers[ev.CarId] = [];

            _drivers[ev.CarId] = driver with
            {
                LastLapTimeMs = ev.LapTimeMs,
                BestLapTimeMs = bestLap,
                TotalLaps = driver.TotalLaps + 1,
                LastLapCuts = ev.Cuts,
                IsInOutLap = false,
            };

            lock (_feedLock)
                _feedEvents.Add(
                    new LapCompletedFeed(
                        DateTime.UtcNow,
                        ev.CarId,
                        driver.DriverName,
                        ev.LapTimeMs,
                        ev.Cuts == 0
                    )
                );
        }

        // Update leaderboard positions from packet
        for (var i = 0; i < ev.Leaderboard.Count; i++)
        {
            var entry = ev.Leaderboard[i];
            if (!_drivers.TryGetValue(entry.CarId, out var entryDriver))
                continue;

            var leaderboardBest = entry.BestLapTimeMs > 0 ? entry.BestLapTimeMs : (int?)null;
            var mergedBest = (leaderboardBest, entryDriver.BestLapTimeMs) switch
            {
                (null, _) => entryDriver.BestLapTimeMs,
                (_, null) => leaderboardBest,
                var (lb, eb) => Math.Min(lb.Value, eb.Value),
            };

            _drivers[entry.CarId] = entryDriver with
            {
                Position = i + 1,
                BestLapTimeMs = mergedBest,
            };
        }
    }

    private void HandleCollision(SimEventCollisionDetected ev)
    {
        IncrementIncident(ev.CarId);
        if (ev.OthercarId.HasValue)
            IncrementIncident(ev.OthercarId.Value);

        var driverName = _drivers.TryGetValue(ev.CarId, out var d)
            ? d.DriverName
            : $"Car #{ev.CarId}";
        string? otherDriverName =
            ev.OthercarId.HasValue && _drivers.TryGetValue(ev.OthercarId.Value, out var od)
                ? od.DriverName
                : null;

        lock (_feedLock)
            _feedEvents.Add(
                new CollisionFeed(
                    ev.OccurredAtUtc,
                    ev.CarId,
                    driverName,
                    ev.OthercarId,
                    otherDriverName,
                    ev.ImpactSpeedKmh
                )
            );
    }

    private void IncrementIncident(int carId)
    {
        if (_drivers.TryGetValue(carId, out var driver))
            _drivers[carId] = driver with { IncidentCount = driver.IncidentCount + 1 };
    }

    private void HandlePitStatus(SimEventPitStatusChanged ev)
    {
        if (_drivers.TryGetValue(ev.CarId, out var driver))
        {
            _drivers.TryUpdate(
                ev.CarId,
                driver with
                {
                    IsInPit = ev.IsInPit,
                    IsInOutLap = ev.IsInPit || driver.IsInOutLap,
                },
                driver
            );
            if (ev.IsInPit)
            {
                lock (_feedLock)
                    _feedEvents.Add(
                        new DriverInPitFeed(DateTime.UtcNow, ev.CarId, driver.DriverName)
                    );
            }
        }
    }

    private void HandleDisconnect(SimEventDriverDisconnected ev)
    {
        if (_drivers.TryGetValue(ev.CarId, out var driver))
        {
            _drivers[ev.CarId] = driver with { IsConnected = false };
            lock (_feedLock)
                _feedEvents.Add(new DriverLeftFeed(DateTime.UtcNow, ev.CarId, driver.DriverName));
        }
    }

    private void HandleSpeedTrapFired(SimEventSpeedTrapFired ev)
    {
        if (_drivers.TryGetValue(ev.CarId, out var driver))
        {
            if (ev.SpeedInKmh > driver.MaxSpeedKmh)
            {
                _drivers.TryUpdate(ev.CarId, driver with { MaxSpeedKmh = ev.SpeedInKmh }, driver);
                lock (_feedLock)
                    _feedEvents.Add(
                        new DriverHitMaxSpeedFeed(
                            DateTime.UtcNow,
                            ev.CarId,
                            driver.DriverName,
                            ev.SpeedInKmh,
                            ev.SpeedTrapName
                        )
                    );
            }
        }
    }

    private static IReadOnlyList<int> UpdateBestSectors(
        IReadOnlyList<int> existing,
        IReadOnlyList<int> newSectors
    )
    {
        var result = new List<int>(3);
        for (var i = 0; i < 3; i++)
        {
            var current = newSectors[i];
            var best = existing.Count > i ? existing[i] : int.MaxValue;
            result.Add(Math.Min(current, best));
        }
        return result;
    }
}
