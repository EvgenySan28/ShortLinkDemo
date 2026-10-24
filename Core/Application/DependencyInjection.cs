using Application.Abstractions;
using Application.ShortLinks;
using Application.ShortLinks.Create;
using Application.ShortLinks.GetOriginalLink;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILinkCodeGenerator, LinkRandomCodeGenerator>();
        services.AddScoped<CreateShortLinkHandler>();
        services.AddScoped<GetOriginalLinkByCodeHandler>();
        return services;
    }
}
