using Devlabs.AcTiming.Application.LiveTiming;
using Devlabs.AcTiming.Application.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devlabs.AcTiming.Infrastructure.AcServer;

/// <summary>
/// Background service that bridges AC UDP events to the LiveTimingService and SignalR.
/// </summary>
public class AcServerBackgroundService : BackgroundService
{
    private readonly IAcUdpClient _udpClient;
    private readonly ILiveTimingService _liveTimingService;
    private readonly ILogger<AcServerBackgroundService> _logger;

    public AcServerBackgroundService(
        IAcUdpClient udpClient,
        ILiveTimingService liveTimingService,
        ILogger<AcServerBackgroundService> logger)
    {
        _udpClient = udpClient;
        _liveTimingService = liveTimingService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _udpClient.SessionStarted += (_, session) =>
        {
            _logger.LogInformation("Session started: {Track} ({Type})", session.TrackName, session.SessionType);
            _liveTimingService.UpdateSession(session);
        };

        _udpClient.SessionEnded += (_, _) =>
        {
            _logger.LogInformation("Session ended");
            _liveTimingService.ClearSession();
        };

        _udpClient.DriverConnected += (_, driver) =>
        {
            _logger.LogInformation("Driver connected: {Name} (Car {CarId})", driver.DriverName, driver.CarId);
            _liveTimingService.UpdateDriver(driver);
        };

        _udpClient.DriverDisconnected += (_, carId) =>
        {
            _logger.LogInformation("Driver disconnected: Car {CarId}", carId);
            _liveTimingService.RemoveDriver(carId);
        };

        _udpClient.DriverUpdated += async (_, driver) =>
        {
            if (_liveTimingService.GetDriver(driver.CarId) is null)
            {
                if (_liveTimingService.GetCurrentSession() is null)
                    await _udpClient.RequestSessionInfoAsync();
                await _udpClient.RequestCarInfoAsync(driver.CarId);
            }
            _liveTimingService.UpdateDriver(driver);
        };

        _udpClient.LapCompleted += (_, evt) =>
        {
            _logger.LogInformation("Lap completed: Car {CarId} - {LapTimeMs}ms (Cuts: {Cuts})", evt.CarId, evt.LapTimeMs, evt.Cuts);

            // Update the completing driver with their latest lap time
            var driver = _liveTimingService.GetDriver(evt.CarId);
            if (driver is not null)
            {
                var bestLap = driver.BestLapTimeMs is null || evt.LapTimeMs < driver.BestLapTimeMs
                    ? evt.LapTimeMs
                    : driver.BestLapTimeMs;

                _liveTimingService.UpdateDriver(driver with
                {
                    LastLapTimeMs = evt.LapTimeMs,
                    BestLapTimeMs = bestLap,
                    TotalLaps = driver.TotalLaps + 1,
                    LastLapCuts = evt.Cuts
                });
            }

            // Update positions and best times from the server leaderboard
            for (var i = 0; i < evt.Leaderboard.Count; i++)
            {
                var entry = evt.Leaderboard[i];
                var entryDriver = _liveTimingService.GetDriver(entry.CarId);
                if (entryDriver is not null)
                {
                    _liveTimingService.UpdateDriver(entryDriver with
                    {
                        Position = i + 1,
                        BestLapTimeMs = entry.BestLapTimeMs > 0 ? entry.BestLapTimeMs : entryDriver.BestLapTimeMs,
                        TotalLaps = entry.TotalLaps
                    });
                }
            }
        };

        await _udpClient.StartAsync(stoppingToken);

        // Keep running until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }

        await _udpClient.StopAsync();
    }
}
