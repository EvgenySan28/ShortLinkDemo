using Domain.ShortLinks;

namespace Application.Abstractions;

public interface IShortLinkRepository
{
    Task<string?> FindCodeByOriginalLink(string originalLink, CancellationToken cancellationToken);

    Task<string?> FindOriginalLinkByCode(string code, CancellationToken cancellationToken);

    Task<TryAddShortLinkResult> TryAdd(ShortLink shortLink, CancellationToken cancellationToken);
}
