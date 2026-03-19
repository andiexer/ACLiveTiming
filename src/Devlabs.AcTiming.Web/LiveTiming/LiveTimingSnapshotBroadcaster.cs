using Devlabs.AcTiming.Application.LiveTiming;

namespace Devlabs.AcTiming.Web.LiveTiming;

public sealed class LiveTimingSnapshotBroadcaster(
    ILiveTimingService liveTimingService,
    LiveTimingBroadcaster broadcaster
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var snapshot = new LiveTimingSnapshot(
                liveTimingService.GetCurrentSession(),
                liveTimingService.GetLeaderboard(),
                liveTimingService.GetFeedEvents()
            );
            await broadcaster.BroadcastAsync(snapshot);
        }
    }
}
