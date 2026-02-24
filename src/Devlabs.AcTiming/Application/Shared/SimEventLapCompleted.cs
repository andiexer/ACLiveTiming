namespace Devlabs.AcTiming.Application.Shared;

public record SimEventLapCompleted(
    int CarId,
    int LapTimeMs,
    int Cuts,
    List<LeaderBoardEntry> Leaderboard
) : SimEvent;

public record LeaderBoardEntry(int CarId, int BestLapTimeMs, int TotalLaps);
