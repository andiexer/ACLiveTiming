namespace Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.Pit;

public sealed class PitStatusTracker
{
    // 6m threshold for being considered "in the pit lane" (squared for distance comparison)
    private const float ThresholdSq = 6f * 6f;

    private (float X, float Z)[]? _points;
    private readonly Dictionary<int, bool> _lastStatus = new();

    public void LoadSpline((float WorldX, float WorldZ)[]? points) => _points = points;

    /// <summary>
    /// Returns the new pit status when it changes, or <c>null</c> if unchanged
    /// (or when no spline is loaded for the current track).
    /// </summary>
    public bool? Process(int carId, float worldX, float worldZ)
    {
        if (_points is not { Length: > 0 })
            return null;

        var inPit = IsNearSpline(worldX, worldZ);

        if (_lastStatus.TryGetValue(carId, out var last) && last == inPit)
            return null;

        _lastStatus[carId] = inPit;
        return inPit;
    }

    public void ResetAll()
    {
        _lastStatus.Clear();
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
