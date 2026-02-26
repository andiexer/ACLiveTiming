using Devlabs.AcTiming.Domain.Shared;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.Pit;

public sealed class PitStatusTracker
{
    // Polygon mode — preferred when a TrackConfig exists
    private IReadOnlyList<WorldPoint>? _polygon;

    private readonly Dictionary<int, bool> _lastStatus = new();

    /// <summary>Load a polygon derived from a <see cref="PitLaneDefinition"/>.</summary>
    public void LoadPolygon(IReadOnlyList<WorldPoint>? polygon) => _polygon = polygon;

    /// <summary>
    /// Returns the new pit status when it changes, or <c>null</c> if unchanged
    /// (or when neither polygon nor spline is loaded).
    /// </summary>
    public bool? Process(int carId, float worldX, float worldZ)
    {
        bool inPit;

        if (_polygon is { Count: > 0 })
            inPit = PolygonHelper.Contains(_polygon, worldX, worldZ);
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
    }

    public void ResetCar(int carId) => _lastStatus.Remove(carId);
}
