namespace Devlabs.AcTiming.Domain.LiveTiming;

/// <summary>Positional and telemetry data carried by a CarUpdate packet.</summary>
public record DriverTelemetry
{
    public int CarId { get; init; }
    public float SplinePosition { get; init; }
    public float WorldX { get; init; }
    public float WorldZ { get; init; }
    public float SpeedKmh { get; init; }
    public int Gear { get; init; }
    public int EngineRpm { get; init; }
}
