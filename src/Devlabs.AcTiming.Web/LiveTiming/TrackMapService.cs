using System.Collections.Concurrent;
using System.Globalization;

namespace Devlabs.AcTiming.Web.LiveTiming;

/// <summary>
/// Reads and caches map.ini configuration from wwwroot/maps/{trackSlug}/map.ini.
/// AC map.ini format: key=value pairs under a [PARAMETERS] section.
/// </summary>
public sealed class TrackMapService
{
    private readonly string _mapsRoot;
    private readonly ConcurrentDictionary<string, TrackMapConfig?> _cache = new(
        StringComparer.OrdinalIgnoreCase
    );

    public TrackMapService(IWebHostEnvironment env)
    {
        _mapsRoot = Path.Combine(env.WebRootPath ?? "", "maps");
    }

    public TrackMapConfig? GetConfig(string? trackName, string? trackConfig)
    {
        if (string.IsNullOrWhiteSpace(trackName))
            return null;

        var fullSlug = string.IsNullOrWhiteSpace(trackConfig)
            ? trackName
            : $"{trackName}/{trackConfig}";

        return _cache.GetOrAdd(fullSlug, LoadConfig(trackName, trackConfig));
    }

    private TrackMapConfig? LoadConfig(string trackName, string? trackConfig)
    {
        var sanitizedTrackName = trackName.Split("/")[^1];
        var fullSlug = string.IsNullOrWhiteSpace(trackConfig)
            ? sanitizedTrackName
            : $"{sanitizedTrackName}/{trackConfig}";

        var iniPath = Path.Combine(_mapsRoot, fullSlug, "map.ini");
        if (!File.Exists(iniPath))
            return null;

        var dict = File.ReadAllLines(iniPath)
            .Select(l => l.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

        float Get(string key, float fallback) =>
            dict.TryGetValue(key, out var v)
            && float.TryParse(v, CultureInfo.InvariantCulture, out var f)
                ? f
                : fallback;

        var width = Get("WIDTH", 875f);
        var height = Get("HEIGHT", 710f);
        var scale = Get("SCALE_FACTOR", 1f);
        var xOffset = Get("X_OFFSET", 0f);
        var zOffset = Get("Z_OFFSET", 0f);
        var margin = Get("MARGIN", 0f);
        var drawingSize = (int)Get("DRAWING_SIZE", 10f);
        var hasImage = File.Exists(Path.Combine(_mapsRoot, fullSlug, "map.png"));

        return new TrackMapConfig(
            sanitizedTrackName,
            trackConfig,
            width,
            height,
            scale,
            xOffset,
            zOffset,
            margin,
            drawingSize,
            hasImage
        );
    }
}

public sealed record TrackMapConfig(
    string TrackName,
    string? TrackConfig,
    float Width,
    float Height,
    float ScaleFactor,
    float XOffset,
    float ZOffset,
    float Margin,
    int DrawingSize,
    bool HasImage
)
{
    // WIDTH/HEIGHT in AC map.ini are the pixel dimensions of the rendered PNG.
    // SCALE_FACTOR is only used in the coordinate transform.
    // The correct transform is: px = (world + offset) * scale + margin
    // (AC's map renderer adds MARGIN pixels of padding around the track content)
    public int PixelWidth => (int)Math.Round(Width);
    public int PixelHeight => (int)Math.Round(Height);
    public string FullTrackName =>
        string.IsNullOrEmpty(TrackConfig) ? TrackName : $"{TrackName}/{TrackConfig}";
}
