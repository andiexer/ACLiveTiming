namespace Devlabs.AcTiming.Domain.Shared;

/// <summary>
/// A speed-measurement line across the track defined by two world-space points.
/// </summary>
public sealed class SpeedTrapDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display name shown in the UI and future analytics (e.g. "Main Straight").</summary>
    public string Name { get; set; } = "";

    /// <summary>First end-point of the measurement line.</summary>
    public WorldPoint Point1 { get; set; } = new(0f, 0f);

    /// <summary>Second end-point of the measurement line.</summary>
    public WorldPoint Point2 { get; set; } = new(0f, 0f);
}
