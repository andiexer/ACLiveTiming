using Devlabs.AcTiming.Domain.Shared;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.Pit;

/// <summary>
/// Pit lane defined as a centre-line polyline plus a half-width.
/// The actual detection polygon is computed on demand via <see cref="ToPolygon"/>.
/// </summary>
public sealed class PitLaneDefinition
{
    /// <summary>Ordered world-space points along the centre of the pit lane.</summary>
    public List<WorldPoint> CenterLine { get; set; } = [];

    /// <summary>Half-width of the pit corridor in metres (e.g. 8 → 16 m total width).</summary>
    public float HalfWidthMeters { get; set; } = 8f;

    /// <summary>
    /// Expands the centre-line into a closed corridor polygon.
    /// Returns an empty list when the centre-line has fewer than 2 points.
    /// </summary>
    public IReadOnlyList<WorldPoint> ToPolygon()
    {
        var pts = CenterLine;
        if (pts.Count < 2)
            return [];

        var n = pts.Count;
        var normals = new (float Nx, float Nz)[n];

        // Compute inward normal at every centre-line vertex
        for (var i = 0; i < n; i++)
        {
            (float nx, float nz) segNormal(int from, int to)
            {
                var dx = pts[to].X - pts[from].X;
                var dz = pts[to].Z - pts[from].Z;
                var len = MathF.Sqrt(dx * dx + dz * dz);
                if (len < 1e-6f)
                    return (0f, 0f);
                // Perpendicular (rotate 90°): (-dz, dx)
                return (-dz / len, dx / len);
            }

            if (i == 0)
                normals[i] = segNormal(0, 1);
            else if (i == n - 1)
                normals[i] = segNormal(n - 2, n - 1);
            else
            {
                // Average the two adjacent segment normals
                var (ax, az) = segNormal(i - 1, i);
                var (bx, bz) = segNormal(i, i + 1);
                var mx = (ax + bx) * 0.5f;
                var mz = (az + bz) * 0.5f;
                var mLen = MathF.Sqrt(mx * mx + mz * mz);
                normals[i] = mLen > 1e-6f ? (mx / mLen, mz / mLen) : (ax, az);
            }
        }

        // Right side (forward pass) + left side (reverse pass) = closed polygon
        var polygon = new List<WorldPoint>(n * 2);

        for (var i = 0; i < n; i++)
            polygon.Add(
                new WorldPoint(
                    pts[i].X + normals[i].Nx * HalfWidthMeters,
                    pts[i].Z + normals[i].Nz * HalfWidthMeters
                )
            );

        for (var i = n - 1; i >= 0; i--)
            polygon.Add(
                new WorldPoint(
                    pts[i].X - normals[i].Nx * HalfWidthMeters,
                    pts[i].Z - normals[i].Nz * HalfWidthMeters
                )
            );

        return polygon;
    }
}
