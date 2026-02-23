using Devlabs.AcTiming.Application.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devlabs.AcTiming.Application.LiveTiming;

public sealed class SimEventRouter(
    ILogger<SimEventRouter> logger,
    ISimEventSource source,
    RealtimeBus realtimeBus,
    PersistenceBus persistenceBus
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var ev in source.ReadSimEventsAsync(ct))
            {
                try
                {
                    logger.LogDebug("Routing event: {EventType}", ev.GetType().Name);
                    realtimeBus.Writer.TryWrite(ev);

                    // TODO: forward to persistence channel if event is needed for historical data (e.g. lap completed, session started/ended)
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error routing event {EventType}", ev.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // normal shutdown — not an error
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SimEventRouter crashed");
        }
        finally
        {
            realtimeBus.Writer.TryComplete();
            persistenceBus.Writer.TryComplete();
        }
    }
}
