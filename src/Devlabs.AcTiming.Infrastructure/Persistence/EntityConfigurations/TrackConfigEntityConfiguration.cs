using Devlabs.AcTiming.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devlabs.AcTiming.Infrastructure.Persistence.EntityConfigurations;

internal sealed class TrackConfigEntityConfiguration : IEntityTypeConfiguration<TrackConfig>
{
    public void Configure(EntityTypeBuilder<TrackConfig> builder)
    {
        builder.HasKey(e => e.Id);

        // 1-to-1 with Track
        builder
            .HasOne(e => e.Track)
            .WithOne(t => t.TrackConfig)
            .HasForeignKey<TrackConfig>(e => e.TrackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.TrackId).IsUnique();

        // PitLaneDefinition stored as a single JSON column.
        // Nullable: column is NULL when no pit lane is configured yet.
        builder.OwnsOne(
            e => e.PitLane,
            pit =>
            {
                pit.ToJson("PitLane");
                pit.Property(p => p.HalfWidthMeters);
                pit.OwnsMany(
                    p => p.CenterLine,
                    wp =>
                    {
                        wp.Property(w => w.X);
                        wp.Property(w => w.Z);
                    }
                );
            }
        );

        // SpeedTrapDefinitions stored as a single JSON array column.
        builder.OwnsMany(
            e => e.SpeedTraps,
            trap =>
            {
                trap.ToJson("SpeedTraps");
                trap.Property(t => t.Id);
                trap.Property(t => t.Name);
                trap.OwnsOne(
                    t => t.Point1,
                    wp =>
                    {
                        wp.Property(w => w.X);
                        wp.Property(w => w.Z);
                    }
                );
                trap.OwnsOne(
                    t => t.Point2,
                    wp =>
                    {
                        wp.Property(w => w.X);
                        wp.Property(w => w.Z);
                    }
                );
            }
        );
    }
}
