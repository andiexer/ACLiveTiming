namespace Devlabs.AcTiming.Application.Shared;

public record SimEventSpeedTrapFired(
    int CarId,
    Guid SpeedTrapId,
    string SpeedTrapName,
    float SpeedInKmh
) : SimEvent;
