using Devlabs.AcTiming.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devlabs.AcTiming.Infrastructure.Persistence.EntityConfigurations;

internal sealed class DriverEntityConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Guid).IsRequired();
        builder.Property(e => e.Name).IsRequired();

        // Drivers are identified cross-session by their AC GUID
        builder.HasIndex(e => e.Guid).IsUnique();
    }
}
