using Application.Abstractions;
using Domain.ShortLinks;
using Microsoft.Extensions.Logging;

namespace Application.ShortLinks.Create;

public sealed record CreateShortLinkCommand(string? OriginalLink);

public abstract record CreateShortLinkResult
{
    public sealed record Created(string Code, string OriginalLink) : CreateShortLinkResult;

    public sealed record AlreadyExists(string Code, string OriginalLink) : CreateShortLinkResult;

    public sealed record Invalid(string Message) : CreateShortLinkResult;
}

public sealed class CreateShortLinkHandler(
    IShortLinkRepository repository,
    ILinkCodeGenerator codeGenerator,
    TimeProvider timeProvider,
    ILogger<CreateShortLinkHandler> logger)
{
    public const int MaxCodeAttempts = 5;

    public async Task<CreateShortLinkResult> Handle(
        CreateShortLinkCommand command,
        CancellationToken cancellationToken)
    {
        if (!OriginalLinkRules.TryNormalize(command.OriginalLink, out var normalized, out var error))
        {
            logger.LogWarning("Create short link rejected: {Error}", error);
            return new CreateShortLinkResult.Invalid(error);
        }

        var existingCode = await repository.FindCodeByOriginalLink(normalized, cancellationToken);
        if (existingCode is not null)
        {
            logger.LogInformation(
                "Short link already exists for {OriginalLink} with code {Code}.",
                normalized,
                existingCode);
            return new CreateShortLinkResult.AlreadyExists(existingCode, normalized);
        }

        for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
        {
            var link = ShortLink.Create(normalized, codeGenerator.Generate(), timeProvider.GetUtcNow().UtcDateTime);
            switch (await repository.TryAdd(link, cancellationToken))
            {
                case TryAddShortLinkResult.Added:
                    logger.LogInformation(
                        "Created short link {Code} for {OriginalLink}.",
                        link.Code,
                        link.OriginalLink);
                    return new CreateShortLinkResult.Created(link.Code, link.OriginalLink);
                case TryAddShortLinkResult.CodeTaken:
                    logger.LogWarning(
                        "Short code {Code} is already taken. Attempt {Attempt} of {MaxAttempts}.",
                        link.Code,
                        attempt + 1,
                        MaxCodeAttempts);
                    continue;
                case TryAddShortLinkResult.OriginalLinkTaken:
                    var winnerCode = await repository.FindCodeByOriginalLink(normalized, cancellationToken);
                    if (winnerCode is null)
                    {
                        logger.LogError(
                            "Conflicting short link for {OriginalLink} was not found after a unique violation.",
                            normalized);
                        throw new InvalidOperationException("A conflicting short link was not found.");
                    }

                    logger.LogInformation(
                        "Short link already exists for {OriginalLink} with code {Code} after a concurrent insert.",
                        normalized,
                        winnerCode);
                    return new CreateShortLinkResult.AlreadyExists(winnerCode, normalized);
            }
        }

        logger.LogError(
            "Failed to allocate a unique short code for {OriginalLink} after {MaxAttempts} attempts.",
            normalized,
            MaxCodeAttempts);
        throw new InvalidOperationException("Failed to allocate a unique short code.");
    }
}
