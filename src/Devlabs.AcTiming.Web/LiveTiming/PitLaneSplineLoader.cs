using System.Collections.Concurrent;
using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Infrastructure.AcServer;

namespace Devlabs.AcTiming.Web.LiveTiming;

/// <summary>
/// Reads and caches pit lane spline points from wwwroot/maps/{track}/pit_lane.ai.
/// </summary>
/// <remarks>
/// Binary format (version 7):
///   Header  : int32 version, int32 count, 16 bytes padding  = 24 bytes
///   Per record (N = (fileSize - 24) / 20):
///     float worldZ, float cumDist, float unused, float worldX, float elevY
/// Copy pit_lane.ai from content/tracks/{track}/ai/ into wwwroot/maps/{track}/.
/// </remarks>
public sealed class PitLaneSplineLoader(IWebHostEnvironment env) : IPitLaneProvider
{
    private readonly string _mapsRoot = Path.Combine(env.WebRootPath ?? "", "maps");
    private readonly ConcurrentDictionary<string, (float WorldX, float WorldZ)[]?> _cache = new(
        StringComparer.OrdinalIgnoreCase
    );

    public (float WorldX, float WorldZ)[]? LoadPoints(string trackName, string? trackConfig)
    {
        var sanitized = TrackNameSanitizer.Sanitize(trackName);
        var slug = string.IsNullOrWhiteSpace(trackConfig)
            ? sanitized
            : $"{sanitized}/{trackConfig}";

        return _cache.GetOrAdd(slug, _ => ParseFile(slug));
    }

    private (float WorldX, float WorldZ)[]? ParseFile(string slug)
    {
        var path = Path.Combine(_mapsRoot, slug, "pit_lane.ai");
        if (!File.Exists(path))
            return null;

        using var fs = File.OpenRead(path);
        if (fs.Length < 24)
            return null;

        using var reader = new BinaryReader(fs);

        // 24-byte header: version(4) + count(4) + padding(16)
        reader.ReadBytes(24);

        var recordCount = (int)((fs.Length - 24) / 20);
        if (recordCount <= 0)
            return null;

        var points = new (float WorldX, float WorldZ)[recordCount];
        for (var i = 0; i < recordCount; i++)
        {
            var worldZ = reader.ReadSingle(); // f[0]
            reader.ReadSingle(); // f[1] cumulative distance
            reader.ReadSingle(); // f[2] unused
            var worldX = reader.ReadSingle(); // f[3]
            reader.ReadSingle(); // f[4] elevation
            points[i] = (worldX, worldZ);
        }

        return points;
    }
}
