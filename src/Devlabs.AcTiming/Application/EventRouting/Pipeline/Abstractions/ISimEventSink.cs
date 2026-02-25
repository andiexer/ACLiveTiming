using Devlabs.AcTiming.Application.Shared;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline.Abstractions;

public interface ISimEventSink
{
    ValueTask PublishAsync(SimEvent ev, CancellationToken ct);
}
