using Application.Abstractions;
using Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Data;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgres = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        var redis = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        services.AddDbContext<ShortLinkDbContext>(options => options.UseNpgsql(postgres));
        services.AddStackExchangeRedisCache(cache =>
        {
            cache.Configuration = redis;
            cache.InstanceName = "shortlink:";
        });
        services.AddScoped<IShortLinkRepository, ShortLinkRepository>();
        services.Decorate<IShortLinkRepository, CachingShortLinkRepository>();
        return services;
    }
}
