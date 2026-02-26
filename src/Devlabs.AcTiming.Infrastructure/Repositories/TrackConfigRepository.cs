using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.Shared;
using Devlabs.AcTiming.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Devlabs.AcTiming.Infrastructure.Repositories;

internal sealed class TrackConfigRepository(AcTimingDbContext db) : ITrackConfigRepository
{
    public async Task<TrackConfig?> FindByTrackAsync(
        string trackName,
        string? trackConfig,
        CancellationToken ct = default
    )
    {
        return await db
            .TrackConfigs.Include(c => c.Track)
            .FirstOrDefaultAsync(
                c => c.Track.Name == trackName && c.Track.Config == trackConfig,
                ct
            );
    }

    public async Task SaveAsync(TrackConfig config, CancellationToken ct = default)
    {
        // Ensure the referenced Track row exists
        if (config.TrackId == 0)
        {
            var track =
                config.Track
                ?? throw new InvalidOperationException(
                    "TrackConfig.Track must be set when TrackId is 0."
                );

            var existing = await db.Tracks.FirstOrDefaultAsync(
                t => t.Name == track.Name && t.Config == track.Config,
                ct
            );

            if (existing is null)
            {
                db.Tracks.Add(track);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                config.TrackId = existing.Id;
                config.Track = existing;
            }
        }

        if (config.Id == 0)
            db.TrackConfigs.Add(config);
        else
            db.TrackConfigs.Update(config);

        await db.SaveChangesAsync(ct);
    }
}
