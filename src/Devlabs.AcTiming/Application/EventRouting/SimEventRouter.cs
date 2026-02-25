using Devlabs.AcTiming.Application.EventRouting.Enricher;
using Devlabs.AcTiming.Application.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devlabs.AcTiming.Application.EventRouting;

public sealed class SimEventRouter(
    ILogger<SimEventRouter> logger,
    ISimEventSource source,
    RealtimeBus realtimeBus,
    PersistenceBus persistenceBus,
    IEnumerable<ISimEventEnricher> enrichers
) : BackgroundService
{
    private readonly ISimEventEnricher[] _preEnrichers = enrichers
        .Where(e => e.Phase == EnricherPhase.Pre)
        .ToArray();

    private readonly ISimEventEnricher[] _postEnrichers = enrichers
        .Where(e => e.Phase == EnricherPhase.Post)
        .ToArray();

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var ev in source.ReadSimEventsAsync(ct))
            {
                try
                {
                    logger.LogDebug("Routing event: {EventType}", ev.GetType().Name);

                    // pre-enrichers
                    foreach (var pre in _preEnrichers)
                    {
                        foreach (var preEvent in await pre.EnrichAsync(ev, ct))
                        {
                            Publish(preEvent);
                        }
                    }

                    // main event
                    Publish(ev);

                    // post-enrichers
                    foreach (var post in _postEnrichers)
                    {
                        foreach (var postEvent in await post.EnrichAsync(ev, ct))
                        {
                            Publish(postEvent);
                        }
                    }
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

    private void Publish(SimEvent ev)
    {
        realtimeBus.Writer.TryWrite(ev);
        persistenceBus.Writer.TryWrite(ev);
    }
}
