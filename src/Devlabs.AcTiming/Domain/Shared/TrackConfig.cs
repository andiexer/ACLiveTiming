using Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.Pit;

namespace Devlabs.AcTiming.Domain.Shared;

/// <summary>
/// Persisted POI configuration for a specific track (1-to-1 with <see cref="Track"/>).
/// </summary>
public sealed class TrackConfig : Entity
{
    public int TrackId { get; set; }
    public Track Track { get; set; } = null!;

    /// <summary>Manually drawn pit lane corridor. Null when not yet configured.</summary>
    public PitLaneDefinition? PitLane { get; set; }

    /// <summary>Speed trap lines on this track.</summary>
    public List<SpeedTrapDefinition> SpeedTraps { get; set; } = [];
}
