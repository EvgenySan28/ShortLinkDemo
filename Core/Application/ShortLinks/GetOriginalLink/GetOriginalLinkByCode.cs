using Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Application.ShortLinks.GetOriginalLink;

public sealed record GetOriginalLinkByCodeCommand(string? Code);

public abstract record GetOriginalLinkByCodeResult
{
    public sealed record Found(string OriginalLink) : GetOriginalLinkByCodeResult;

    public sealed record NotFound : GetOriginalLinkByCodeResult;
}

public sealed class GetOriginalLinkByCodeHandler(
    IShortLinkRepository repository,
    ILogger<GetOriginalLinkByCodeHandler> logger)
{
    public async Task<GetOriginalLinkByCodeResult> Handle(
        GetOriginalLinkByCodeCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
        {
            logger.LogWarning("Redirect rejected: short code is missing.");
            return new GetOriginalLinkByCodeResult.NotFound();
        }

        var link = await repository.FindOriginalLinkByCode(command.Code, cancellationToken);
        if (link is null)
        {
            logger.LogWarning("Short link {Code} was not found.", command.Code);
            return new GetOriginalLinkByCodeResult.NotFound();
        }

        var incremented = await repository.IncrementRedirectCount(command.Code, cancellationToken);
        if (!incremented)
            logger.LogWarning("Short link {Code} was not found while counting the redirect.", command.Code);

        logger.LogInformation("Resolved short link {Code} to {OriginalLink}.", command.Code, link);
        return new GetOriginalLinkByCodeResult.Found(link);
    }
}
