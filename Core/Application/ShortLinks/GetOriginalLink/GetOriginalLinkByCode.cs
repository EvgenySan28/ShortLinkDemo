using Application.Abstractions;

namespace Application.ShortLinks.GetOriginalLink;

public sealed record GetOriginalLinkByCodeCommand(string? Code);

public abstract record GetOriginalLinkByCodeResult
{
    public sealed record Found(string OriginalLink) : GetOriginalLinkByCodeResult;

    public sealed record NotFound : GetOriginalLinkByCodeResult;
}

public sealed class GetOriginalLinkByCodeHandler(IShortLinkRepository repository)
{
    public async Task<GetOriginalLinkByCodeResult> Handle(
        GetOriginalLinkByCodeCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
            return new GetOriginalLinkByCodeResult.NotFound();

        var link = await repository.FindOriginalLinkByCode(command.Code, cancellationToken);
        return link is null
            ? new GetOriginalLinkByCodeResult.NotFound()
            : new GetOriginalLinkByCodeResult.Found(link);
    }
}
