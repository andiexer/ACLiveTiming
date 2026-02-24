namespace Devlabs.AcTiming.Application.Shared;

public record SimEventSectorCrossed(
    int CarId,
    int SectorIndex,
    int SectorTimeMs,
    IReadOnlyList<int> CompletedSectorsThisLap,
    bool IsValidLap
) : SimEvent;
