using Devlabs.AcTiming.Domain.Shared;

namespace Devlabs.AcTiming.Application.Shared;

public interface ITrackConfigRepository
{
    /// <summary>
    /// Returns the saved config for a track, or <c>null</c> if none exists yet.
    /// </summary>
    Task<TrackConfig?> FindByTrackAsync(
        string trackName,
        string? trackConfig,
        CancellationToken ct = default
    );

    /// <summary>
    /// Inserts or updates the config.
    /// When <see cref="TrackConfig.Id"/> is 0 the entity is new; otherwise it is updated.
    /// The associated <see cref="Track"/> row is created automatically if it does not exist.
    /// </summary>
    Task SaveAsync(TrackConfig config, CancellationToken ct = default);
}
