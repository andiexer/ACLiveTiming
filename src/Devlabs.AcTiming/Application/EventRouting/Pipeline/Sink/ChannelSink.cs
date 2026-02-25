using Devlabs.AcTiming.Application.EventRouting.Pipeline.Abstractions;
using Devlabs.AcTiming.Application.Shared;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline.Sink;

public sealed class ChannelSink(RealtimeBus realtimeBus, PersistenceBus persistenceBus)
    : ISimEventSink
{
    public ValueTask PublishAsync(SimEvent ev, CancellationToken ct)
    {
        realtimeBus.Writer.TryWrite(ev);
        persistenceBus.Writer.TryWrite(ev);
        return ValueTask.CompletedTask;
    }
}
