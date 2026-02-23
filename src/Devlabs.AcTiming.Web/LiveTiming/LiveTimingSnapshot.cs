using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Web.LiveTiming;

public record LiveTimingSnapshot(
    LiveSessionInfo? Session,
    IReadOnlyList<LiveDriverEntry> Leaderboard
);
