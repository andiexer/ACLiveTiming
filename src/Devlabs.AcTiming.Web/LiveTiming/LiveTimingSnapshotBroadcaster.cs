using Devlabs.AcTiming.Application.LiveTiming;
using Microsoft.AspNetCore.SignalR;

namespace Devlabs.AcTiming.Web.LiveTiming;

public sealed class LiveTimingSnapshotBroadcaster(
    ILiveTimingService liveTimingService,
    IHubContext<LiveTimingHub> hubContext
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var snapshot = new LiveTimingSnapshot(
                liveTimingService.GetCurrentSession(),
                liveTimingService.GetLeaderboard()
            );
            await hubContext.Clients.All.SendAsync(
                LiveTimingHubMethods.StateSnapshot,
                snapshot,
                stoppingToken
            );
        }
    }
}
