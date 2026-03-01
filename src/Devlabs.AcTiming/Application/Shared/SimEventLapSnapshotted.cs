using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Application.Shared;

/// <summary>
/// Emitted by <see cref="LapTelemetryEnricher"/> as a pre-enricher side-effect immediately before
/// <see cref="SimEventLapCompleted"/> hits the sink. Carries the telemetry buffer collected during
/// the just-finished lap so both the realtime and persistence consumers receive it without
/// maintaining their own buffers.
/// </summary>
public record SimEventLapSnapshotted(
    int CarId,
    IReadOnlyList<LapTelemetrySample> Samples,
    float MaxSpeedKmh
) : SimEvent;
