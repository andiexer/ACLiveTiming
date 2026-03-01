using Devlabs.AcTiming.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devlabs.AcTiming.Infrastructure.Persistence.EntityConfigurations;

internal sealed class SessionEntityConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ServerName).IsRequired();
        builder.Property(e => e.ClosedReason).IsRequired(false);

        builder.HasOne(e => e.Track).WithMany(t => t.Sessions).HasForeignKey(e => e.TrackId);

        builder.HasMany(e => e.Laps).WithOne(l => l.Session).HasForeignKey(l => l.SessionId);

        builder.HasMany(e => e.Drivers).WithMany(d => d.Sessions).UsingEntity("DriverSession");

        builder.HasIndex(e => e.TrackId);

        // Fast lookup for open sessions during session-resume check
        builder.HasIndex(e => e.EndedAtUtc);
    }
}
