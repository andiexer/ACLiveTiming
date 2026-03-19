using Devlabs.AcTiming.Application.LiveTiming;

namespace Devlabs.AcTiming.Web.LiveTiming;

/// <summary>
/// Singleton service that distributes live timing snapshots directly to subscribed
/// Blazor components via C# callbacks — no SignalR hub required.
/// </summary>
public sealed class LiveTimingBroadcaster
{
    private readonly List<Func<LiveTimingSnapshot, Task>> _handlers = [];
    private readonly Lock _lock = new();

    public void Subscribe(Func<LiveTimingSnapshot, Task> handler)
    {
        lock (_lock)
            _handlers.Add(handler);
    }

    public void Unsubscribe(Func<LiveTimingSnapshot, Task> handler)
    {
        lock (_lock)
            _handlers.Remove(handler);
    }

    internal async Task BroadcastAsync(LiveTimingSnapshot snapshot)
    {
        List<Func<LiveTimingSnapshot, Task>> handlers;
        lock (_lock)
            handlers = [.. _handlers];

        await Task.WhenAll(
            handlers.Select(async h =>
            {
                try
                {
                    await h(snapshot);
                }
                catch
                { /* component disposed or faulted — ignore */
                }
            })
        );
    }
}
