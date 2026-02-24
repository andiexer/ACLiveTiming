using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Web.LiveTiming;

public record LiveTimingSnapshot(
    SessionInfo? Session,
    IReadOnlyList<LiveDriver> Leaderboard,
    IReadOnlyList<SessionFeedEvent> FeedEvents
);
