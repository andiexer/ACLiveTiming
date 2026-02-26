namespace Devlabs.AcTiming.Domain.LiveTiming;

/// <summary>A single telemetry snapshot captured during a lap, keyed by spline position.</summary>
public sealed record LapTelemetrySample(
    float SplinePosition,
    float WorldX,
    float WorldZ,
    float SpeedKmh,
    int Gear
);
