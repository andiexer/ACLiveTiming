using System.Threading.Channels;
using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Application.Shared;

public sealed class RealtimeBus
{
    public Channel<SimEvent> Channel { get; }
    public ChannelWriter<SimEvent> Writer => Channel.Writer;
    public ChannelReader<SimEvent> Reader => Channel.Reader;

    public RealtimeBus(Channel<SimEvent> channel) => Channel = channel;
}

public sealed class PersistenceBus
{
    public Channel<SimEvent> Channel { get; }
    public ChannelWriter<SimEvent> Writer => Channel.Writer;
    public ChannelReader<SimEvent> Reader => Channel.Reader;

    public PersistenceBus(Channel<SimEvent> channel) => Channel = channel;
}
