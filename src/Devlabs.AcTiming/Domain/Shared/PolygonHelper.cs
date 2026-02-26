namespace Devlabs.AcTiming.Domain.Shared;

/// <summary>
/// 2-D geometry utilities for world-space polygons.
/// </summary>
public static class PolygonHelper
{
    /// <summary>
    /// Ray-casting point-in-polygon test.
    /// Returns <c>true</c> when (<paramref name="x"/>, <paramref name="z"/>) is inside
    /// the polygon (points on the boundary are treated as inside).
    /// </summary>
    public static bool Contains(IReadOnlyList<WorldPoint> polygon, float x, float z)
    {
        var n = polygon.Count;
        if (n < 3)
            return false;

        var inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var xi = polygon[i].X;
            var zi = polygon[i].Z;
            var xj = polygon[j].X;
            var zj = polygon[j].Z;

            if ((zi > z) != (zj > z) && x < (xj - xi) * (z - zi) / (zj - zi) + xi)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
