using System.Collections.Concurrent;
using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Application.LiveTiming;

public sealed class LiveTimingSession
{
    private readonly ConcurrentDictionary<int, LiveDriver> _drivers = new();
    private readonly Lock _feedLock = new();
    private readonly List<SessionFeedEvent> _feedEvents = [];

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
            case SimEventCollisionDetected c:
                HandleCollision(c);
                break;
            case SimEventDriverDisconnected d:
                HandleDisconnect(d);
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
            var bestLap =
                ev.Cuts == 0
                && (driver.BestLapTimeMs is null || ev.LapTimeMs < driver.BestLapTimeMs)
                    ? ev.LapTimeMs
                    : driver.BestLapTimeMs;

            _drivers[ev.CarId] = driver with
            {
                LastLapTimeMs = ev.LapTimeMs,
                BestLapTimeMs = bestLap,
                TotalLaps = driver.TotalLaps + 1,
                LastLapCuts = ev.Cuts,
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

    private void HandleDisconnect(SimEventDriverDisconnected ev)
    {
        if (_drivers.TryGetValue(ev.CarId, out var driver))
        {
            _drivers[ev.CarId] = driver with { IsConnected = false };
            lock (_feedLock)
                _feedEvents.Add(new DriverLeftFeed(DateTime.UtcNow, ev.CarId, driver.DriverName));
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
