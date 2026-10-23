using Application.Abstractions;
using Domain.ShortLinks;

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
    TimeProvider timeProvider)
{
    public const int MaxCodeAttempts = 5;

    public async Task<CreateShortLinkResult> Handle(
        CreateShortLinkCommand command,
        CancellationToken cancellationToken)
    {
        if (!OriginalLinkRules.TryNormalize(command.OriginalLink, out var normalized, out var error))
            return new CreateShortLinkResult.Invalid(error);

        var existing = await repository.FindByOriginalLink(normalized, cancellationToken);
        if (existing is not null)
            return new CreateShortLinkResult.AlreadyExists(existing.Code, existing.OriginalLink);

        for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
        {
            var link = ShortLink.Create(normalized, codeGenerator.Generate(), timeProvider.GetUtcNow().UtcDateTime);
            switch (await repository.TryAdd(link, cancellationToken))
            {
                case TryAddShortLinkResult.Added:
                    return new CreateShortLinkResult.Created(link.Code, link.OriginalLink);
                case TryAddShortLinkResult.CodeTaken:
                    continue;
                case TryAddShortLinkResult.OriginalLinkTaken:
                    var winner = await repository.FindByOriginalLink(normalized, cancellationToken);
                    if (winner is null)
                        throw new InvalidOperationException("A conflicting short link was not found.");

                    return new CreateShortLinkResult.AlreadyExists(winner.Code, winner.OriginalLink);
            }
        }

        throw new InvalidOperationException("Failed to allocate a unique short code.");
    }
}
