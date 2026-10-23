using Data.Entities;
using Data.Entities.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Data;

public class ShortLinkDbContext(DbContextOptions<ShortLinkDbContext> options) : DbContext(options)
{
    public DbSet<ShortLink> ShortLinks { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ShortLinkConfiguration());
    }
}
