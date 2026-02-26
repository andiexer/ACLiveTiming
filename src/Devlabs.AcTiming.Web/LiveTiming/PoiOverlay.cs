using Devlabs.AcTiming.Domain.Shared;

namespace Devlabs.AcTiming.Web.LiveTiming;

/// <summary>Base type for all SVG overlays rendered on top of the track map.</summary>
public abstract record PoiOverlay;

/// <summary>
/// Renders the pit lane corridor as a semi-transparent polygon.
/// Pass <see cref="PitLaneDefinition.ToPolygon"/> as the polygon.
/// </summary>
public sealed record PitLaneOverlay(IReadOnlyList<WorldPoint> Polygon) : PoiOverlay;

/// <summary>Renders a speed trap measurement line across the track.</summary>
public sealed record SpeedTrapOverlay(string Name, WorldPoint Point1, WorldPoint Point2)
    : PoiOverlay;

/// <summary>
/// Renders a recorded or in-progress polyline path.
/// Used both for driver path recording in the configurator and future lap comparison.
/// </summary>
public sealed record PathOverlay(
    IReadOnlyList<WorldPoint> Points,
    string Color = "#4ecca3",
    float Opacity = 0.75f
) : PoiOverlay;
