using System.Collections.Concurrent;
using Devlabs.AcTiming.Application.LiveTiming;
using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Infrastructure.Services;

public class LiveTimingService : ILiveTimingService
{
    private readonly ConcurrentDictionary<int, LiveDriverEntry> _drivers = new();
    private LiveSessionInfo? _currentSession;

    public LiveSessionInfo? GetCurrentSession() => _currentSession;

    public LiveDriverEntry? GetDriver(int carId) =>
        _drivers.GetValueOrDefault(carId);

    public IReadOnlyList<LiveDriverEntry> GetLeaderboard() =>
        _drivers.Values
            .Where(d => d.IsConnected)
            .OrderBy(d => d.Position)
            .ToList();

    public void UpdateSession(LiveSessionInfo session)
    {
        _currentSession = session;
    }

    public void UpdateDriver(LiveDriverEntry driver)
    {
        _drivers.AddOrUpdate(driver.CarId, driver, (_, existing) =>
        {
            // CarUpdate packets only carry spline position - merge into existing
            if (string.IsNullOrEmpty(driver.DriverName))
            {
                return existing with
                {
                    SplinePosition = driver.SplinePosition,
                    WorldX = driver.WorldX,
                    WorldZ = driver.WorldZ,
                    SpeedKmh = driver.SpeedKmh,
                    Gear = driver.Gear,
                    EngineRpm = driver.EngineRpm,
                    IsConnected = driver.IsConnected
                };
            }

            // All other updates: merge onto existing, preserving fields the caller didn't set
            return existing with
            {
                DriverName = driver.DriverName,
                DriverGuid = !string.IsNullOrEmpty(driver.DriverGuid) ? driver.DriverGuid : existing.DriverGuid,
                Team = driver.Team ?? existing.Team,
                CarModel = !string.IsNullOrEmpty(driver.CarModel) ? driver.CarModel : existing.CarModel,
                CarSkin = driver.CarSkin ?? existing.CarSkin,
                IsConnected = driver.IsConnected,
                BestLapTimeMs = driver.BestLapTimeMs ?? existing.BestLapTimeMs,
                LastLapTimeMs = driver.LastLapTimeMs ?? existing.LastLapTimeMs,
                LastLapCuts = driver.LastLapTimeMs is not null ? driver.LastLapCuts : existing.LastLapCuts,
                TotalLaps = driver.TotalLaps > 0 ? driver.TotalLaps : existing.TotalLaps,
                Position = driver.Position > 0 ? driver.Position : existing.Position,
                SplinePosition = driver.SplinePosition != 0 ? driver.SplinePosition : existing.SplinePosition,
                LastSectorTimesMs = driver.LastSectorTimesMs.Count > 0 ? driver.LastSectorTimesMs : existing.LastSectorTimesMs,
                BestSectorTimesMs = driver.BestSectorTimesMs.Count > 0 ? driver.BestSectorTimesMs : existing.BestSectorTimesMs
            };
        });
    }

    public void RemoveDriver(int carId)
    {
        if (_drivers.TryGetValue(carId, out var driver))
        {
            _drivers[carId] = driver with { IsConnected = false };
        }
    }

    public void ClearSession()
    {
        _currentSession = null;
        _drivers.Clear();
    }
}
