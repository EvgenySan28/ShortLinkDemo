using Application.Abstractions;
using Data.Options;
using Domain.ShortLinks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Data.Persistence;

internal sealed class CachingShortLinkRepository(
    IShortLinkRepository repository,
    IDistributedCache cache,
    IOptions<RedirectCacheOptions> options) : IShortLinkRepository
{
    private const string KeyPrefix = "redirect:";

    public Task<string?> FindCodeByOriginalLink(string originalLink, CancellationToken cancellationToken)
    {
        return repository.FindCodeByOriginalLink(originalLink, cancellationToken);
    }

    public async Task<string?> FindOriginalLinkByCode(string code, CancellationToken cancellationToken)
    {
        var key = KeyPrefix + code;
        var cached = await cache.GetStringAsync(key, cancellationToken);
        if (cached is not null)
            return cached;

        var originalLink = await repository.FindOriginalLinkByCode(code, cancellationToken);
        if (originalLink is null)
            return null;

        await cache.SetStringAsync(
            key,
            originalLink,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = options.Value.Duration
            },
            cancellationToken);

        return originalLink;
    }

    public Task<TryAddShortLinkResult> TryAdd(ShortLink shortLink, CancellationToken cancellationToken)
    {
        return repository.TryAdd(shortLink, cancellationToken);
    }
}
