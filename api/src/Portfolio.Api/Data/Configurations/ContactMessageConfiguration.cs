using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Api.Domain;

namespace Portfolio.Api.Data.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        // Os mesmos limites que ContactRequest valida na entrada.
        builder.Property(m => m.Name).HasMaxLength(100);
        builder.Property(m => m.Email).HasMaxLength(254);
        builder.Property(m => m.Message).HasMaxLength(4000);
    }
}
