namespace Devlabs.AcTiming.Application.LiveTiming;

/// <summary>
/// Tracks per-car sector progress from normalized spline position.
/// Sectors split at 1/3 and 2/3 of track length.
/// Handles warp (pit teleport), backwards driving, and lap wraps.
/// </summary>
public sealed class SectorTimingTracker
{
    private static readonly float[] Boundaries = [1f / 3f, 2f / 3f];

    /// <summary>Forward jump > 30% of track in one tick = pit warp / teleport.</summary>
    private const float WarpThreshold = 0.3f;

    /// <summary>Backward movement > 5% = driving the wrong way, ignore crossings.</summary>
    private const float BackwardThreshold = -0.05f;

    public sealed record SectorCrossing(
        int SectorIndex,
        int SectorTimeMs,
        List<int> CompletedSectors // all sectors done so far this lap, e.g. [s1] or [s1, s2]
    );

    private sealed class CarState
    {
        public int CurrentSector;
        public DateTime SectorStartTime = DateTime.UtcNow;
        public float PreviousSpline = -1f;
        public DateTime PreviousUpdateTime = DateTime.UtcNow;
        public int S1Ms;
        public int S2Ms;
    }

    private readonly Dictionary<int, CarState> _states = new();

    /// <summary>
    /// Process a new spline position for a car.
    /// Returns a <see cref="SectorCrossing"/> when a sector boundary is crossed, null otherwise.
    /// </summary>
    public SectorCrossing? ProcessUpdate(int carId, float spline)
    {
        var now = DateTime.UtcNow;

        if (!_states.TryGetValue(carId, out var state))
        {
            _states[carId] = new CarState
            {
                PreviousSpline = spline,
                PreviousUpdateTime = now,
                CurrentSector = SectorOf(spline),
                SectorStartTime = now,
            };
            return null;
        }

        var prev = state.PreviousSpline;
        var prevTime = state.PreviousUpdateTime;
        var delta = spline - prev;

        state.PreviousSpline = spline;
        state.PreviousUpdateTime = now;

        // Lap wrap (spline ~1 → ~0): reset sector index for the new lap, but keep S1Ms/S2Ms
        // so that OnLapCompleted (arriving shortly after) can still compute S3.
        if (delta < -0.5f)
        {
            state.CurrentSector = 0;
            state.SectorStartTime = now;
            return null;
        }

        // Pit warp (large forward jump) or significant backward movement
        if (delta > WarpThreshold || delta < BackwardThreshold)
        {
            state.CurrentSector = SectorOf(spline);
            state.SectorStartTime = now;
            state.S1Ms = 0;
            state.S2Ms = 0;
            return null;
        }

        // Check each sector boundary in order
        for (var i = 0; i < Boundaries.Length; i++)
        {
            if (state.CurrentSector != i)
                continue;
            if (prev >= Boundaries[i] || spline < Boundaries[i])
                continue;

            // Interpolate crossing time between the two updates for sub-tick accuracy
            var fraction = delta > 0f ? (Boundaries[i] - prev) / delta : 0f;
            var elapsed = (now - prevTime).TotalMilliseconds;
            var crossingTime = prevTime.AddMilliseconds(fraction * elapsed);
            var sectorMs = Math.Max(
                1,
                (int)(crossingTime - state.SectorStartTime).TotalMilliseconds
            );

            if (i == 0)
                state.S1Ms = sectorMs;
            else
                state.S2Ms = sectorMs;

            state.CurrentSector = i + 1;
            state.SectorStartTime = crossingTime;

            var completedSectors =
                i == 0 ? new List<int> { state.S1Ms } : new List<int> { state.S1Ms, state.S2Ms };

            return new SectorCrossing(i, sectorMs, completedSectors);
        }

        return null;
    }

    /// <summary>
    /// Finalizes the lap, computing S3 = lapTimeMs - S1 - S2.
    /// Returns [S1, S2, S3] if all sectors were captured cleanly, null otherwise.
    /// </summary>
    public int[]? OnLapCompleted(int carId, int lapTimeMs)
    {
        if (!_states.TryGetValue(carId, out var state))
            return null;

        var s1 = state.S1Ms;
        var s2 = state.S2Ms;

        state.CurrentSector = 0;
        state.SectorStartTime = DateTime.UtcNow;
        state.S1Ms = 0;
        state.S2Ms = 0;

        if (s1 <= 0 || s2 <= 0)
            return null;

        var s3 = lapTimeMs - s1 - s2;
        if (s3 <= 0)
            return null;

        return [s1, s2, s3];
    }

    public void ResetCar(int carId) => _states.Remove(carId);

    public void ResetAll() => _states.Clear();

    private static int SectorOf(float spline) =>
        spline < Boundaries[0] ? 0
        : spline < Boundaries[1] ? 1
        : 2;
}
