using Devlabs.AcTiming.Domain.Shared;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.SpeedTrap;

/// <summary>
/// Detects when a car's movement between two telemetry frames crosses a speed-trap line.
///
/// Detection principle — segment-segment intersection:
///   Each telemetry update gives the car a new world position.  The straight line
///   from the *previous* position to the *current* position is the "movement segment".
///   A trap is triggered when that segment intersects the trap's line segment
///   (Point1 → Point2).
///
///   The intersection test uses 2-D cross products (X/Z plane):
///     cross(u, v) = u.x·v.z − u.z·v.x
///   Points A and B straddle line CD when cross(CD, CA) and cross(CD, CB) have
///   opposite signs.  Straddling must hold for *both* pairs, which confines the
///   crossing to the finite segments rather than their infinite extensions.
/// </summary>
public sealed class SpeedTrapTracker
{
    private readonly Dictionary<int, (float X, float Z)> _lastPos = new();
    private IReadOnlyList<SpeedTrapDefinition> _traps = [];

    public void LoadTraps(IReadOnlyList<SpeedTrapDefinition> traps) => _traps = traps;

    public void ResetAll()
    {
        _lastPos.Clear();
        _traps = [];
    }

    public void ResetCar(int carId) => _lastPos.Remove(carId);

    /// <summary>
    /// Call on every <c>SimEventTelemetryUpdated</c>.
    /// Returns the triggered trap and the car's speed at that moment,
    /// or <c>null</c> when no crossing occurred.
    /// Only the first trap crossed in a single frame is returned; in practice
    /// traps shouldn't overlap.
    /// </summary>
    public (SpeedTrapDefinition Trap, float SpeedKmh)? Process(
        int carId,
        float worldX,
        float worldZ,
        float speedKmh
    )
    {
        var current = (worldX, worldZ);

        if (!_lastPos.TryGetValue(carId, out var prev))
        {
            // First frame for this car — record position, no crossing possible yet.
            _lastPos[carId] = current;
            return null;
        }

        _lastPos[carId] = current;

        foreach (var trap in _traps)
        {
            if (
                SegmentsIntersect(
                    prev.X,
                    prev.Z,
                    current.worldX,
                    current.worldZ,
                    trap.Point1.X,
                    trap.Point1.Z,
                    trap.Point2.X,
                    trap.Point2.Z
                )
            )
            {
                return (trap, speedKmh);
            }
        }

        return null;
    }

    // -------------------------------------------------------------------------
    // Geometry helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns true when segment AB and segment CD intersect.
    /// AB = car movement (prev → current), CD = speed trap (Point1 → Point2).
    /// </summary>
    private static bool SegmentsIntersect(
        float ax,
        float az,
        float bx,
        float bz,
        float cx,
        float cz,
        float dx,
        float dz
    )
    {
        // d1, d2: on which side of CD are A and B?
        var d1 = Cross(dx - cx, dz - cz, ax - cx, az - cz);
        var d2 = Cross(dx - cx, dz - cz, bx - cx, bz - cz);

        // d3, d4: on which side of AB are C and D?
        var d3 = Cross(bx - ax, bz - az, cx - ax, cz - az);
        var d4 = Cross(bx - ax, bz - az, dx - ax, dz - az);

        // Proper crossing: both pairs straddle the opposite segment.
        return OppositeSigns(d1, d2) && OppositeSigns(d3, d4);
    }

    /// <summary>2-D cross product: u × v = u.x·v.z − u.z·v.x</summary>
    private static float Cross(float ux, float uz, float vx, float vz) => ux * vz - uz * vx;

    private static bool OppositeSigns(float a, float b) => (a > 0f && b < 0f) || (a < 0f && b > 0f);
}
