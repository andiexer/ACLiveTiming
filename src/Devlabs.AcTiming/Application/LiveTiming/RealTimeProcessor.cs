using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.LiveTiming;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devlabs.AcTiming.Application.LiveTiming;

public sealed class RealTimeProcessor(
    ILogger<RealTimeProcessor> logger,
    RealtimeBus realtimeBus,
    ISimEventSource simEventSource,
    ILiveTimingService liveTimingService
) : BackgroundService
{
    private readonly SectorTimingTracker _sectorTracker = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RealTimeProcessor started");
        await ConsumeEventsAsync(stoppingToken);
        logger.LogInformation("RealTimeProcessor stopped");
    }

    private async Task ConsumeEventsAsync(CancellationToken ct)
    {
        await foreach (var ev in realtimeBus.Reader.ReadAllAsync(ct))
        {
            try
            {
                switch (ev)
                {
                    case LiveSessionInfo s:
                        HandleSessionStarted(s);
                        await simEventSource.SendRealtimePosIntervalAsync();
                        break;

                    case LiveSessionEnded:
                        HandleSessionEnded();
                        break;

                    case LiveDriverEntry d:
                        HandleDriverConnected(d);
                        break;

                    case DriverDisconnected d:
                        HandleDriverDisconnected(d);
                        break;

                    case DriverTelemetry t:
                        await HandleDriverTelemetryAsync(t);
                        break;

                    case LapCompletedEvent l:
                        HandleLapCompleted(l);
                        break;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling realtime event {EventType}", ev.GetType().Name);
            }
        }
    }

    private void HandleSessionStarted(LiveSessionInfo session)
    {
        logger.LogInformation(
            "Session started: {Track} ({Type})",
            session.TrackName,
            session.SessionType
        );
        _sectorTracker.ResetAll();
        liveTimingService.UpdateSession(session);
    }

    private void HandleSessionEnded()
    {
        logger.LogInformation("Session ended");
        _sectorTracker.ResetAll();
        liveTimingService.ClearSession();
    }

    private void HandleDriverConnected(LiveDriverEntry driver)
    {
        logger.LogInformation(
            "Driver connected: {Name} (Car {CarId})",
            driver.DriverName,
            driver.CarId
        );
        liveTimingService.UpdateDriver(driver);
    }

    private void HandleDriverDisconnected(DriverDisconnected disconnected)
    {
        logger.LogInformation("Driver disconnected: Car {CarId}", disconnected.CarId);
        _sectorTracker.ResetCar(disconnected.CarId);
        liveTimingService.RemoveDriver(disconnected.CarId);
    }

    private async Task HandleDriverTelemetryAsync(DriverTelemetry telemetry)
    {
        if (liveTimingService.GetDriver(telemetry.CarId) is null)
        {
            if (liveTimingService.GetCurrentSession() is null)
                await simEventSource.RequestSessionInfoAsync();

            await simEventSource.RequestCarInfoAsync(telemetry.CarId);
            return;
        }

        liveTimingService.UpdateDriverTelemetry(telemetry);

        var crossing = _sectorTracker.ProcessUpdate(telemetry.CarId, telemetry.SplinePosition);
        if (crossing is not null)
        {
            var driver = liveTimingService.GetDriver(telemetry.CarId);
            if (driver is not null)
                liveTimingService.UpdateDriver(
                    driver with
                    {
                        LastSectorTimesMs = crossing.CompletedSectors,
                    }
                );
        }
    }

    private void HandleLapCompleted(LapCompletedEvent evt)
    {
        logger.LogInformation(
            "Lap completed: Car {CarId} - {LapTimeMs}ms (Cuts: {Cuts})",
            evt.CarId,
            evt.LapTimeMs,
            evt.Cuts
        );

        var driver = liveTimingService.GetDriver(evt.CarId);
        if (driver is not null)
        {
            var bestLap =
                evt.Cuts == 0
                && (driver.BestLapTimeMs is null || evt.LapTimeMs < driver.BestLapTimeMs)
                    ? evt.LapTimeMs
                    : driver.BestLapTimeMs;

            var sectors = _sectorTracker.OnLapCompleted(evt.CarId, evt.LapTimeMs);
            var lastSectors = sectors?.ToList() ?? driver.LastSectorTimesMs;
            var bestSectors =
                sectors is not null && evt.Cuts == 0
                    ? UpdateBestSectors(driver.BestSectorTimesMs, sectors)
                    : driver.BestSectorTimesMs;

            liveTimingService.UpdateDriver(
                driver with
                {
                    LastLapTimeMs = evt.LapTimeMs,
                    BestLapTimeMs = bestLap,
                    TotalLaps = driver.TotalLaps + 1,
                    LastLapCuts = evt.Cuts,
                    LastSectorTimesMs = lastSectors,
                    BestSectorTimesMs = bestSectors,
                }
            );
        }

        for (var i = 0; i < evt.Leaderboard.Count; i++)
        {
            var entry = evt.Leaderboard[i];
            var entryDriver = liveTimingService.GetDriver(entry.CarId);
            if (entryDriver is null)
                continue;

            var leaderboardBest = entry.BestLapTimeMs > 0 ? entry.BestLapTimeMs : (int?)null;
            var mergedBest = (leaderboardBest, entryDriver.BestLapTimeMs) switch
            {
                (null, _) => entryDriver.BestLapTimeMs,
                (_, null) => leaderboardBest,
                var (lb, eb) => Math.Min(lb.Value, eb.Value),
            };

            liveTimingService.UpdateDriver(
                entryDriver with
                {
                    Position = i + 1,
                    BestLapTimeMs = mergedBest,
                }
            );
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
