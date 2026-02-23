using Devlabs.AcTiming.Application.LiveTiming;
using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.LiveTiming;
using Microsoft.AspNetCore.SignalR;

// POC combined processor + notifier. currently hard refactoring ongoing
// realtimeprocessor will live in application layer, and only depend on simevents + live timing service (no hub context).
// another hosted service for hub notifications with different interval logic (e.g. session updates every 5s, driver updates every 100ms, lap updates immediately) will live in the web layer
namespace Devlabs.AcTiming.Web.Hubs;

public sealed class RealTimeProcessor(
    ILogger<RealTimeProcessor> logger,
    RealtimeBus realtimeBus,
    ISimEventSource simEventSource,
    ILiveTimingService liveTimingService,
    IHubContext<TimingHub> hubContext
) : BackgroundService
{
    private readonly SectorTimingTracker _sectorTracker = new();

    // Optional: tiny per-car throttle to avoid spamming UI (keep UI unchanged).
    // Set to TimeSpan.Zero to disable.
    private static readonly TimeSpan DriverUiMinInterval = TimeSpan.Zero; // e.g. TimeSpan.FromMilliseconds(100);
    private readonly Dictionary<int, DateTimeOffset> _lastDriverUiPush = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RealTimeProcessorHubNotifier started");

        await foreach (var ev in realtimeBus.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                switch (ev)
                {
                    case LiveSessionInfo s:
                        HandleSessionStarted(s, stoppingToken);
                        await simEventSource.SendRealtimePosIntervalAsync();
                        break;

                    case LiveSessionEnded ended:
                        HandleSessionEnded(ended, stoppingToken);
                        break;

                    case LiveDriverEntry d:
                        HandleDriverConnected(d, stoppingToken);
                        break;

                    case DriverDisconnected d:
                        await HandleDriverDisconnectedAsync(d, stoppingToken);
                        break;

                    case DriverTelemetry t:
                        await HandleDriverTelemetryAsync(t, stoppingToken);
                        break;

                    case LapCompletedEvent l:
                        await HandleLapCompletedAsync(l, stoppingToken);
                        break;

                    case CollisionEvent c:
                        await hubContext.Clients.All.SendAsync(TimingHubMethods.CollisionOccurred, c, stoppingToken);
                        break;

                    default:
                        // unknown/ignored event type
                        break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling realtime event {EventType}", ev.GetType().Name);
            }
        }

        logger.LogInformation("RealTimeProcessorHubNotifier stopped");
    }

    private void HandleSessionStarted(LiveSessionInfo session, CancellationToken ct)
    {
        logger.LogInformation("Session started: {Track} ({Type})", session.TrackName, session.SessionType);

        _sectorTracker.ResetAll();
        liveTimingService.UpdateSession(session);

        // UI
        _ = hubContext.Clients.All.SendAsync(TimingHubMethods.SessionUpdated, session, ct);
    }

    private void HandleSessionEnded(LiveSessionEnded _ended, CancellationToken ct)
    {
        logger.LogInformation("Session ended");

        _sectorTracker.ResetAll();
        liveTimingService.ClearSession();

        // UI
        _ = hubContext.Clients.All.SendAsync(TimingHubMethods.SessionUpdated, (LiveSessionInfo?)null, ct);
    }

    private void HandleDriverConnected(LiveDriverEntry driver, CancellationToken ct)
    {
        logger.LogInformation("Driver connected: {Name} (Car {CarId})", driver.DriverName, driver.CarId);

        liveTimingService.UpdateDriver(driver);

        // UI sends merged state (same behavior as your old notifier)
        var merged = liveTimingService.GetDriver(driver.CarId) ?? driver;
        _ = hubContext.Clients.All.SendAsync(TimingHubMethods.DriverUpdated, merged, ct);
    }

    private async Task HandleDriverDisconnectedAsync(DriverDisconnected disconnected, CancellationToken ct)
    {
        logger.LogInformation("Driver disconnected: Car {CarId}", disconnected.CarId);

        _sectorTracker.ResetCar(disconnected.CarId);
        liveTimingService.RemoveDriver(disconnected.CarId);

        await hubContext.Clients.All.SendAsync(TimingHubMethods.DriverDisconnected, disconnected.CarId, ct);
    }

    private async Task HandleDriverTelemetryAsync(DriverTelemetry telemetry, CancellationToken ct)
    {
        // stale-data recovery: telemetry before driver/session known
        if (liveTimingService.GetDriver(telemetry.CarId) is null)
        {
            if (liveTimingService.GetCurrentSession() is null)
                await simEventSource.RequestSessionInfoAsync();

            await simEventSource.RequestCarInfoAsync(telemetry.CarId);
            return;
        }

        liveTimingService.UpdateDriverTelemetry(telemetry);

        // Sector crossing tracking
        var crossing = _sectorTracker.ProcessUpdate(telemetry.CarId, telemetry.SplinePosition);
        if (crossing is not null)
        {
            var driver = liveTimingService.GetDriver(telemetry.CarId);
            if (driver is not null)
            {
                liveTimingService.UpdateDriver(driver with
                {
                    LastSectorTimesMs = crossing.CompletedSectors
                });
            }
        }

        // UI: push merged state (throttle optional)
        if (ShouldPushDriverToUi(telemetry.CarId))
        {
            var merged = liveTimingService.GetDriver(telemetry.CarId);
            if (merged is not null)
                await hubContext.Clients.All.SendAsync(TimingHubMethods.DriverUpdated, merged, ct);
        }
    }

    private async Task HandleLapCompletedAsync(LapCompletedEvent evt, CancellationToken ct)
    {
        logger.LogInformation("Lap completed: Car {CarId} - {LapTimeMs}ms (Cuts: {Cuts})",
            evt.CarId, evt.LapTimeMs, evt.Cuts);

        var driver = liveTimingService.GetDriver(evt.CarId);
        if (driver is not null)
        {
            var bestLap = evt.Cuts == 0 && (driver.BestLapTimeMs is null || evt.LapTimeMs < driver.BestLapTimeMs)
                ? evt.LapTimeMs
                : driver.BestLapTimeMs;

            var sectors = _sectorTracker.OnLapCompleted(evt.CarId, evt.LapTimeMs);
            var lastSectors = sectors?.ToList() ?? driver.LastSectorTimesMs;

            var bestSectors = sectors is not null && evt.Cuts == 0
                ? UpdateBestSectors(driver.BestSectorTimesMs, sectors)
                : driver.BestSectorTimesMs;

            liveTimingService.UpdateDriver(driver with
            {
                LastLapTimeMs = evt.LapTimeMs,
                BestLapTimeMs = bestLap,
                TotalLaps = driver.TotalLaps + 1,
                LastLapCuts = evt.Cuts,
                LastSectorTimesMs = lastSectors,
                BestSectorTimesMs = bestSectors
            });
        }

        // Merge leaderboard snapshot from event (AC sends bests+laps)
        for (var i = 0; i < evt.Leaderboard.Count; i++)
        {
            var entry = evt.Leaderboard[i];
            var entryDriver = liveTimingService.GetDriver(entry.CarId);
            if (entryDriver is null) continue;

            var leaderboardBest = entry.BestLapTimeMs > 0 ? entry.BestLapTimeMs : (int?)null;
            var mergedBest = (leaderboardBest, entryDriver.BestLapTimeMs) switch
            {
                (null, _)    => entryDriver.BestLapTimeMs,
                (_, null)    => leaderboardBest,
                var (lb, eb) => Math.Min(lb.Value, eb.Value)
            };

            liveTimingService.UpdateDriver(entryDriver with
            {
                Position = i + 1,
                BestLapTimeMs = mergedBest,
            });
        }

        // UI: send full updated leaderboard (same as old notifier)
        var leaderboard = liveTimingService.GetLeaderboard();
        await hubContext.Clients.All.SendAsync(TimingHubMethods.LeaderboardUpdated, leaderboard, ct);
    }

    private bool ShouldPushDriverToUi(int carId)
    {
        if (DriverUiMinInterval <= TimeSpan.Zero) return true;

        var now = DateTimeOffset.UtcNow;
        if (_lastDriverUiPush.TryGetValue(carId, out var last) && (now - last) < DriverUiMinInterval)
            return false;

        _lastDriverUiPush[carId] = now;
        return true;
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