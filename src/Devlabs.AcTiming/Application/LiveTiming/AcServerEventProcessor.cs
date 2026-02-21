using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.LiveTiming;
using Microsoft.Extensions.Logging;

namespace Devlabs.AcTiming.Application.LiveTiming;

/// <summary>
/// Subscribes to raw AC UDP events and applies application-level business logic
/// (stale-data recovery, best-lap calculation, leaderboard merging, sector timing)
/// before updating the in-memory live timing state.
/// </summary>
public sealed class AcServerEventProcessor
{
    private readonly IAcUdpClient _udpClient;
    private readonly ILiveTimingService _liveTimingService;
    private readonly ILogger<AcServerEventProcessor> _logger;
    private readonly SectorTimingTracker _sectorTracker = new();

    public AcServerEventProcessor(
        IAcUdpClient udpClient,
        ILiveTimingService liveTimingService,
        ILogger<AcServerEventProcessor> logger)
    {
        _udpClient = udpClient;
        _liveTimingService = liveTimingService;
        _logger = logger;

        _udpClient.SessionStarted += OnSessionStarted;
        _udpClient.SessionEnded += OnSessionEnded;
        _udpClient.DriverConnected += OnDriverConnected;
        _udpClient.DriverDisconnected += OnDriverDisconnected;
        _udpClient.DriverUpdated += OnDriverUpdated;
        _udpClient.LapCompleted += OnLapCompleted;
    }

    private void OnSessionStarted(object? sender, LiveSessionInfo session)
    {
        _logger.LogInformation("Session started: {Track} ({Type})", session.TrackName, session.SessionType);
        _sectorTracker.ResetAll();
        _liveTimingService.UpdateSession(session);
    }

    private void OnSessionEnded(object? sender, EventArgs _)
    {
        _logger.LogInformation("Session ended");
        _sectorTracker.ResetAll();
        _liveTimingService.ClearSession();
    }

    private void OnDriverConnected(object? sender, LiveDriverEntry driver)
    {
        _logger.LogInformation("Driver connected: {Name} (Car {CarId})", driver.DriverName, driver.CarId);
        _liveTimingService.UpdateDriver(driver);
    }

    private void OnDriverDisconnected(object? sender, int carId)
    {
        _logger.LogInformation("Driver disconnected: Car {CarId}", carId);
        _sectorTracker.ResetCar(carId);
        _liveTimingService.RemoveDriver(carId);
    }

    // async void is required for event handlers; exceptions are caught to avoid crashing the process.
    private async void OnDriverUpdated(object? sender, DriverTelemetry telemetry)
    {
        try
        {
            if (_liveTimingService.GetDriver(telemetry.CarId) is null)
            {
                if (_liveTimingService.GetCurrentSession() is null)
                    await _udpClient.RequestSessionInfoAsync();
                await _udpClient.RequestCarInfoAsync(telemetry.CarId);
                return; // telemetry for an unknown driver is dropped; CarInfo will register them
            }

            _liveTimingService.UpdateDriverTelemetry(telemetry);

            // Check for sector boundary crossing and update live sector display
            var crossing = _sectorTracker.ProcessUpdate(telemetry.CarId, telemetry.SplinePosition);
            if (crossing is not null)
            {
                var driver = _liveTimingService.GetDriver(telemetry.CarId);
                if (driver is not null)
                {
                    _liveTimingService.UpdateDriver(driver with
                    {
                        LastSectorTimesMs = crossing.CompletedSectors
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling DriverUpdated for Car {CarId}", telemetry.CarId);
        }
    }

    private void OnLapCompleted(object? sender, LapCompletedEvent evt)
    {
        _logger.LogInformation("Lap completed: Car {CarId} - {LapTimeMs}ms (Cuts: {Cuts})", evt.CarId, evt.LapTimeMs, evt.Cuts);

        var driver = _liveTimingService.GetDriver(evt.CarId);
        if (driver is not null)
        {
            var bestLap = evt.Cuts == 0 && (driver.BestLapTimeMs is null || evt.LapTimeMs < driver.BestLapTimeMs)
                ? evt.LapTimeMs
                : driver.BestLapTimeMs;

            var sectors = _sectorTracker.OnLapCompleted(evt.CarId, evt.LapTimeMs);
            var lastSectors = sectors?.ToList() ?? driver.LastSectorTimesMs;
            // Only update personal-best sectors on clean laps (no cuts)
            var bestSectors = sectors is not null && evt.Cuts == 0
                ? UpdateBestSectors(driver.BestSectorTimesMs, sectors)
                : driver.BestSectorTimesMs;

            _liveTimingService.UpdateDriver(driver with
            {
                LastLapTimeMs = evt.LapTimeMs,
                BestLapTimeMs = bestLap,
                TotalLaps = driver.TotalLaps + 1,
                LastLapCuts = evt.Cuts,
                LastSectorTimesMs = lastSectors,
                BestSectorTimesMs = bestSectors
            });
        }

        for (var i = 0; i < evt.Leaderboard.Count; i++)
        {
            var entry = evt.Leaderboard[i];
            var entryDriver = _liveTimingService.GetDriver(entry.CarId);
            if (entryDriver is not null)
            {
                var leaderboardBest = entry.BestLapTimeMs > 0 ? entry.BestLapTimeMs : (int?)null;
                var mergedBest = (leaderboardBest, entryDriver.BestLapTimeMs) switch
                {
                    (null, _)    => entryDriver.BestLapTimeMs,
                    (_, null)    => leaderboardBest,
                    var (lb, eb) => Math.Min(lb.Value, eb.Value)
                };
                _liveTimingService.UpdateDriver(entryDriver with
                {
                    Position = i + 1,
                    BestLapTimeMs = mergedBest,
                });
            }
        }
    }

    private static List<int> UpdateBestSectors(List<int> existing, int[] newSectors)
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
