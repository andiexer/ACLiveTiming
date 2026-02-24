using Devlabs.AcTiming.Application.Shared;
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
                    case SimEventSessionInfoReceived s:
                        logger.LogInformation(
                            "Session started: {Track} ({Type})",
                            s.TrackName,
                            s.Type
                        );
                        liveTimingService.StartSession(s);
                        await simEventSource.SendRealtimePosIntervalAsync();
                        break;

                    case SimEventSessionEnded:
                        logger.LogInformation("Session ended");
                        liveTimingService.EndSession();
                        break;

                    case SimEventTelemetryUpdated t:
                        if (liveTimingService.GetCurrentSession() is null)
                            await simEventSource.RequestSessionInfoAsync();
                        else if (liveTimingService.GetDriver(t.CarId) is null)
                            await simEventSource.RequestCarInfoAsync(t.CarId);
                        else
                            liveTimingService.ApplyEvent(t);
                        break;

                    case SimEventDriverConnected d:
                        logger.LogInformation(
                            "Driver connected: {Name} (Car {CarId})",
                            d.DriverName,
                            d.CarId
                        );
                        liveTimingService.ApplyEvent(d);
                        break;

                    case SimEventDriverDisconnected d:
                        logger.LogInformation("Driver disconnected: Car {CarId}", d.CarId);
                        liveTimingService.ApplyEvent(d);
                        break;

                    case SimEventPitStatusChanged p:
                        logger.LogInformation(
                            "Pit status changed: Car {CarId} is now {Status}",
                            p.CarId,
                            p.IsInPit ? "IN PIT" : "ON TRACK"
                        );
                        liveTimingService.ApplyEvent(p);
                        break;

                    default:
                        liveTimingService.ApplyEvent(ev);
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
}
