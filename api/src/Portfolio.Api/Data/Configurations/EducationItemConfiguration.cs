using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Api.Domain;

namespace Portfolio.Api.Data.Configurations;

public class EducationItemConfiguration : IEntityTypeConfiguration<EducationItem>
{
    public void Configure(EntityTypeBuilder<EducationItem> builder)
    {
        builder.Property(e => e.Institution).HasMaxLength(120);
        builder.Property(e => e.Title).HasMaxLength(160);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
    }
}
