using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Api.Domain;

namespace Portfolio.Api.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.Property(p => p.Slug).HasMaxLength(80);
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.Name).HasMaxLength(120);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        // N:N com Technology. O EF cria a tabela de junção ProjectTechnology.
        builder.HasMany(p => p.Technologies)
            .WithMany(t => t.Projects)
            .UsingEntity("ProjectTechnology");
    }
}
