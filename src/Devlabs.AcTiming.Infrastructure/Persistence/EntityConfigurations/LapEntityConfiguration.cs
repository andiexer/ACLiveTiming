using Devlabs.AcTiming.Domain.LiveTiming;
using Devlabs.AcTiming.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devlabs.AcTiming.Infrastructure.Persistence.EntityConfigurations;

internal sealed class LapEntityConfiguration : IEntityTypeConfiguration<Lap>
{
    public void Configure(EntityTypeBuilder<Lap> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Session).WithMany(s => s.Laps).HasForeignKey(e => e.SessionId);

        builder.HasOne(e => e.Driver).WithMany(d => d.Laps).HasForeignKey(e => e.DriverId);

        builder.HasOne(e => e.Car).WithMany(c => c.Laps).HasForeignKey(e => e.CarId);

        builder.HasOne(e => e.Track).WithMany(t => t.Laps).HasForeignKey(e => e.TrackId);

        var sectorComparer = new ValueComparer<List<int>>(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            v => v.Aggregate(0, (a, x) => HashCode.Combine(a, x)),
            v => v.ToList()
        );

        builder
            .Property(e => e.SectorTimesMs)
            .HasConversion(
                v => string.Join(',', v),
                v =>
                    v.Length == 0
                        ? new List<int>()
                        : v.Split(',', StringSplitOptions.None).Select(int.Parse).ToList()
            )
            .Metadata.SetValueComparer(sectorComparer);

        // Telemetry stored as a compact binary BLOB (20 bytes per sample).
        // Never queried column-by-column — always loaded and deserialized as a unit.
        var telemetryComparer = new ValueComparer<List<LapTelemetrySample>>(
            (a, b) => a != null && b != null && a.Count == b.Count,
            v => v.Count,
            v => v.ToList()
        );

        builder
            .Property(e => e.TelemetrySamples)
            .HasColumnType("BLOB")
            .HasConversion(
                v => LapTelemetrySerializer.Serialize(v),
                v => LapTelemetrySerializer.Deserialize(v)
            )
            .Metadata.SetValueComparer(telemetryComparer);

        builder.HasIndex(e => e.SessionId);
        builder.HasIndex(e => e.DriverId);
        builder.HasIndex(e => e.CarId);
        builder.HasIndex(e => e.TrackId);
    }
}
