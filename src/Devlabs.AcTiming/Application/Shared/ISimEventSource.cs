using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Application.Shared;

public interface ISimEventSource
{
    Task RequestCarInfoAsync(int carId);
    Task RequestSessionInfoAsync();
    Task SendRealtimePosIntervalAsync();
    IAsyncEnumerable<SimEvent> ReadSimEventsAsync(CancellationToken ct = default);
}
