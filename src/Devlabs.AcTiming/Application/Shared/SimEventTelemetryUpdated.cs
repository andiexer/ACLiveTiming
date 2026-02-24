namespace Devlabs.AcTiming.Application.Shared;

public record SimEventTelemetryUpdated(
    int CarId,
    float SplinePosition,
    float WorldX,
    float WorldZ,
    float SpeedKmh,
    int Gear,
    int EngineRpm
) : SimEvent;
