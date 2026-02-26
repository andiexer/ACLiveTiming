using Devlabs.AcTiming.Domain.Shared;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.Pit;

public sealed class PitStatusTracker
{
    // Fallback threshold for legacy spline-based detection (squared for perf)
    private const float ThresholdSq = 6f * 6f;

    // Polygon mode — preferred when a TrackConfig exists
    private IReadOnlyList<WorldPoint>? _polygon;

    // Spline mode — fallback when no TrackConfig is saved for the track
    private (float X, float Z)[]? _points;

    private readonly Dictionary<int, bool> _lastStatus = new();

    /// <summary>Load a polygon derived from a <see cref="PitLaneDefinition"/>.</summary>
    public void LoadPolygon(IReadOnlyList<WorldPoint>? polygon) => _polygon = polygon;

    /// <summary>Load a raw spline as fallback (legacy pit_lane.ai data).</summary>
    public void LoadSpline((float WorldX, float WorldZ)[]? points) => _points = points;

    /// <summary>
    /// Returns the new pit status when it changes, or <c>null</c> if unchanged
    /// (or when neither polygon nor spline is loaded).
    /// </summary>
    public bool? Process(int carId, float worldX, float worldZ)
    {
        bool inPit;

        if (_polygon is { Count: > 0 })
            inPit = PolygonHelper.Contains(_polygon, worldX, worldZ);
        else if (_points is { Length: > 0 })
            inPit = IsNearSpline(worldX, worldZ);
        else
            return null;

        if (_lastStatus.TryGetValue(carId, out var last) && last == inPit)
            return null;

        _lastStatus[carId] = inPit;
        return inPit;
    }

    public void ResetAll()
    {
        _lastStatus.Clear();
        _polygon = null;
        _points = null;
    }

    public void ResetCar(int carId) => _lastStatus.Remove(carId);

    private bool IsNearSpline(float worldX, float worldZ)
    {
        foreach (var (px, pz) in _points!)
        {
            var dx = worldX - px;
            var dz = worldZ - pz;
            if (dx * dx + dz * dz <= ThresholdSq)
                return true;
        }
        return false;
    }
}
