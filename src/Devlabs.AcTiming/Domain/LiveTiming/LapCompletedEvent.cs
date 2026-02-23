namespace Devlabs.AcTiming.Domain.LiveTiming;

public record LapCompletedEvent : SimEvent
{
    public int CarId { get; init; }
    public int LapTimeMs { get; init; }
    public int Cuts { get; init; }
    public List<LeaderboardEntry> Leaderboard { get; init; } = [];
}

public record LeaderboardEntry
{
    public int CarId { get; init; }
    public int BestLapTimeMs { get; init; }
    public int TotalLaps { get; init; }
}
