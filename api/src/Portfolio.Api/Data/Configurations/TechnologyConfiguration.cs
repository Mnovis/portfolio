using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Api.Domain;

namespace Portfolio.Api.Data.Configurations;

public class TechnologyConfiguration : IEntityTypeConfiguration<Technology>
{
    public void Configure(EntityTypeBuilder<Technology> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(60);
        builder.HasIndex(t => t.Name).IsUnique();

        builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(20);
    }
}
