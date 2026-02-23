using Microsoft.AspNetCore.SignalR;

namespace Devlabs.AcTiming.Web.LiveTiming;

public class LiveTimingHub : Hub
{
    public const string HubUrl = "/hubs/timing";
}

public static class LiveTimingHubMethods
{
    public const string StateSnapshot = "StateSnapshot";
    public const string CollisionOccurred = "CollisionOccurred"; // TODO: move into StateSnapshot events feed
}
