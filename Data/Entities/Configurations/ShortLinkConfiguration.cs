using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Entities.Configurations;

internal class ShortLinkConfiguration : IEntityTypeConfiguration<ShortLink>
{
    public void Configure(EntityTypeBuilder<ShortLink> builder)
    {
        builder.HasIndex(s => s.OriginalLink)
            .IncludeProperties(s => s.Code)
            .IsUnique(true);

        builder.HasIndex(s => s.Code)
            .IncludeProperties(s => s.OriginalLink)
            .IsUnique(true);
    }
}
