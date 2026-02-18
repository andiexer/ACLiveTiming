using Microsoft.EntityFrameworkCore;

namespace Devlabs.AcTiming.Infrastructure.Persistence;

public class AcTimingDbContext(DbContextOptions<AcTimingDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AcTimingDbContext).Assembly);
    }
}
