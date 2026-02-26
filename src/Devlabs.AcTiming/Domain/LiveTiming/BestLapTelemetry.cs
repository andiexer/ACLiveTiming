namespace Devlabs.AcTiming.Domain.LiveTiming;

/// <summary>Fastest clean lap telemetry for a specific driver + car combination, held in-memory for the current session.</summary>
public sealed record BestLapTelemetry(
    string DriverGuid,
    string DriverName,
    string CarModel,
    int LapTimeMs,
    IReadOnlyList<LapTelemetrySample> Samples
);
