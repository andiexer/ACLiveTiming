using Devlabs.AcTiming.Application.LiveTiming;
using Microsoft.AspNetCore.SignalR;

namespace Devlabs.AcTiming.Web.Hubs;

public class TimingHub(ILiveTimingService liveTimingService) : Hub
{
    public const string HubUrl = "/hubs/timing";

    public async Task RequestFullState()
    {
        var session = liveTimingService.GetCurrentSession();
        if (session is not null)
        {
            await Clients.Caller.SendAsync("SessionUpdated", session);
        }

        var leaderboard = liveTimingService.GetLeaderboard();
        await Clients.Caller.SendAsync("LeaderboardUpdated", leaderboard);
    }
}

public static class TimingHubMethods
{
    public const string SessionUpdated = "SessionUpdated";
    public const string LeaderboardUpdated = "LeaderboardUpdated";
    public const string DriverUpdated = "DriverUpdated";
    public const string DriverDisconnected = "DriverDisconnected";
    public const string LapCompleted = "LapCompleted";
    public const string CollisionOccurred = "CollisionOccurred";
}
