using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.LapTelemetry;

/// <summary>
/// Buffers telemetry samples per car slot across a lap and produces a snapshot when the lap
/// completes. This is the single source of truth for lap telemetry collection — both the
/// realtime and persistence pipelines consume the resulting <c>SimEventLapSnapshot</c>.
/// </summary>
public sealed class LapTelemetryTracker
{
    // 5000 samples is the hard ceiling. The min step only prevents AFK/stationary noise —
    // it's small enough that the cap, not the step, is the binding constraint.
    // At 5000 samples over a Nordschleife lap (~14 500 telemetry updates):
    //   every ~3rd update is stored → one point every ~4 m. Fine-grained enough for analysis.
    // Storage: 5000 × 20 bytes (binary) = 100 KB per lap.
    private const int MaxSamplesPerLap = 5000;

    // 0.0002 of normalized spline ≈ 4 m on Nordschleife, ≈ 1 m on Monza.
    // Purpose: filter out updates where the car is fully stationary (AFK, paused).
    // Does NOT limit resolution for moving cars — the cap above does that.
    private const float MinSplineStep = 0.0002f;

    // Written only from the sequential pipeline — no locking needed.
    private readonly Dictionary<int, List<LapTelemetrySample>> _buffers = new();
    private readonly Dictionary<int, float> _maxSpeeds = new();

    public void ResetAll()
    {
        _buffers.Clear();
        _maxSpeeds.Clear();
    }

    public void ResetCar(int carId)
    {
        _buffers.Remove(carId);
        _maxSpeeds.Remove(carId);
    }

    public void AppendSample(
        int carId,
        float splinePosition,
        float worldX,
        float worldZ,
        float speedKmh,
        int gear
    )
    {
        if (!_buffers.TryGetValue(carId, out var buffer))
        {
            buffer = [];
            _buffers[carId] = buffer;
        }

        if (
            buffer.Count < MaxSamplesPerLap
            && (
                buffer.Count == 0
                || Math.Abs(splinePosition - buffer[^1].SplinePosition) >= MinSplineStep
            )
        )
        {
            buffer.Add(new LapTelemetrySample(splinePosition, worldX, worldZ, speedKmh, gear));
        }

        if (!_maxSpeeds.TryGetValue(carId, out var current) || speedKmh > current)
            _maxSpeeds[carId] = speedKmh;
    }

    /// <summary>
    /// Returns the collected samples and max speed for the completed lap, then resets the car's buffer.
    /// Returns an empty snapshot if no samples were recorded.
    /// </summary>
    public (IReadOnlyList<LapTelemetrySample> Samples, float MaxSpeedKmh) TakeLapSnapshot(int carId)
    {
        _buffers.TryGetValue(carId, out var buffer);
        _maxSpeeds.TryGetValue(carId, out var maxSpeed);

        var samples = buffer is { Count: > 0 }
            ? (IReadOnlyList<LapTelemetrySample>)buffer.ToList()
            : [];

        _buffers[carId] = [];
        _maxSpeeds[carId] = 0f;

        return (samples, maxSpeed);
    }
}
