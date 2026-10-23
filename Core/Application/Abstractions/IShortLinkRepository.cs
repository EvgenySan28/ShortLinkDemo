using Domain.ShortLinks;

namespace Application.Abstractions;

public interface IShortLinkRepository
{
    Task<ShortLink?> FindByOriginalLink(string originalLink, CancellationToken cancellationToken);

    Task<TryAddShortLinkResult> TryAdd(ShortLink shortLink, CancellationToken cancellationToken);
}
