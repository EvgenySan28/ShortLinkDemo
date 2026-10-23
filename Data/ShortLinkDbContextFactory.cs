using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Data;

public sealed class ShortLinkDbContextFactory : IDesignTimeDbContextFactory<ShortLinkDbContext>
{
    public ShortLinkDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.DesignTime.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured. Copy appsettings.DesignTime.json.example to appsettings.DesignTime.json.");

        var options = new DbContextOptionsBuilder<ShortLinkDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ShortLinkDbContext(options);
    }
}
