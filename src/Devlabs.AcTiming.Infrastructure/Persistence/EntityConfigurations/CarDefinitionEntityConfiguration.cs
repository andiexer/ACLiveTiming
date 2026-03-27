using Devlabs.AcTiming.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devlabs.AcTiming.Infrastructure.Persistence.EntityConfigurations;

internal sealed class CarDefinitionEntityConfiguration : IEntityTypeConfiguration<CarDefinition>
{
    public void Configure(EntityTypeBuilder<CarDefinition> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Model).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Brand).HasMaxLength(100);
        builder.Property(e => e.DisplayName).HasMaxLength(200);
        builder.Property(e => e.LogoSlug).HasMaxLength(100);

        builder.HasIndex(e => e.Model).IsUnique();

        // Computed property — not mapped to a column.
        builder.Ignore(e => e.EffectiveSlug);
    }
}
