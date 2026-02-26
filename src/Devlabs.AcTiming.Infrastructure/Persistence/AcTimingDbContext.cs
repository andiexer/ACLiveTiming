using Devlabs.AcTiming.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Devlabs.AcTiming.Infrastructure.Persistence;

public class AcTimingDbContext(DbContextOptions<AcTimingDbContext> options) : DbContext(options)
{
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<TrackConfig> TrackConfigs => Set<TrackConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AcTimingDbContext).Assembly);
    }
}
