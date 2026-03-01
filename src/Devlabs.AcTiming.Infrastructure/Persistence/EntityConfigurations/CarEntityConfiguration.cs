using Devlabs.AcTiming.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devlabs.AcTiming.Infrastructure.Persistence.EntityConfigurations;

internal sealed class CarEntityConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Model).IsRequired();

        // A car is uniquely identified by its model + skin combination
        builder.HasIndex(e => new { e.Model, e.Skin }).IsUnique();
    }
}
