using Devlabs.AcTiming.Application.Shared;

namespace Devlabs.AcTiming.Application.Abstractions;

public interface ISimEventSource
{
    Task RequestCarInfoAsync(int carId);
    Task RequestSessionInfoAsync();
    Task SendRealtimePosIntervalAsync();
    IAsyncEnumerable<SimEvent> ReadSimEventsAsync(CancellationToken ct = default);
}
