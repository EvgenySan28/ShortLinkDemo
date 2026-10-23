using Domain.ShortLinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Entities.Configurations;

internal class ShortLinkConfiguration : IEntityTypeConfiguration<ShortLink>
{
    public void Configure(EntityTypeBuilder<ShortLink> builder)
    {
        builder.Property(s => s.OriginalLink)
            .HasMaxLength(OriginalLinkRules.MaxLength);

        builder.Property(s => s.Code)
            .HasMaxLength(8);

        builder.HasIndex(s => s.OriginalLink)
            .IncludeProperties(s => s.Code)
            .IsUnique(true);

        builder.HasIndex(s => s.Code)
            .IncludeProperties(s => s.OriginalLink)
            .IsUnique(true);
    }
}
