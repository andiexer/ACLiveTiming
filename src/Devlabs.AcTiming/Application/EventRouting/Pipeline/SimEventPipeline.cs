using Devlabs.AcTiming.Application.EventRouting.Pipeline.Abstractions;
using Devlabs.AcTiming.Application.Shared;
using Microsoft.Extensions.Logging;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline;

public sealed class SimEventPipeline(
    ILogger<SimEventPipeline> logger,
    IEnumerable<ISimEventEnricher> enrichers,
    ISimEventSink sink
)
{
    private readonly ISimEventEnricher[] _preEnrichers = enrichers
        .Where(e => e.Phase == EnricherPhase.Pre)
        .ToArray();

    private readonly ISimEventEnricher[] _postEnrichers = enrichers
        .Where(e => e.Phase == EnricherPhase.Post)
        .ToArray();

    public async Task RouteAsync(SimEvent ev, CancellationToken ct)
    {
        try
        {
            // pre-enrichers
            await RunEnrichersAsync(_preEnrichers, ev, ct);

            // main event
            await sink.PublishAsync(ev, ct);

            // post-enrichers
            await RunEnrichersAsync(_postEnrichers, ev, ct);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error routing event {EventType}", ev.GetType().Name);
        }
    }

    private async Task RunEnrichersAsync(
        IEnumerable<ISimEventEnricher> enrichers,
        SimEvent ev,
        CancellationToken ct
    )
    {
        foreach (var enricher in enrichers)
        {
            try
            {
                foreach (var enrichedEvent in await enricher.EnrichAsync(ev, ct))
                {
                    await sink.PublishAsync(enrichedEvent, ct);
                }
            }
            catch (Exception e)
            {
                logger.LogError(
                    e,
                    "Error in enricher {EnricherType} for event {EventType}",
                    enricher.GetType().Name,
                    ev.GetType().Name
                );
            }
        }
    }
}
