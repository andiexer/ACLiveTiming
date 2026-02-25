using Devlabs.AcTiming.Application.Shared;

namespace Devlabs.AcTiming.Application.EventRouting.Enricher;

public interface ISimEventEnricher
{
    EnricherPhase Phase { get; }
    ValueTask<IReadOnlyList<SimEvent>> EnrichAsync(SimEvent @event, CancellationToken ct);
}

public enum EnricherPhase
{
    Pre,
    Post,
}
