using Application.Abstractions;
using Domain.ShortLinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Data.Persistence;

internal sealed class ShortLinkRepository(ShortLinkDbContext db, ILogger<ShortLinkRepository> logger) : IShortLinkRepository
{
    private const string CodeUniqueIndex = "IX_ShortLinks_Code";
    private const string OriginalLinkUniqueIndex = "IX_ShortLinks_OriginalLink";

    public Task<string?> FindCodeByOriginalLink(string originalLink, CancellationToken cancellationToken)
    {
        return db.ShortLinks
            .AsNoTracking()
            .Where(link => link.OriginalLink == originalLink)
            .Select(link => link.Code)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<string?> FindOriginalLinkByCode(string code, CancellationToken cancellationToken)
    {
        return db.ShortLinks
            .AsNoTracking()
            .Where(link => link.Code == code)
            .Select(link => link.OriginalLink)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IncrementRedirectCount(string code, CancellationToken cancellationToken)
    {
        var updated = await db.ShortLinks
            .Where(link => link.Code == code)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(link => link.RedirectCount, link => link.RedirectCount + 1),
                cancellationToken);

        return updated == 1;
    }

    public async Task<TryAddShortLinkResult> TryAdd(ShortLink shortLink, CancellationToken cancellationToken)
    {
        db.ShortLinks.Add(shortLink);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Saved short link {Code}.", shortLink.Code);
            return TryAddShortLinkResult.Added;
        }
        catch (DbUpdateException exception) when (FindUniqueViolation(exception) is { } violation)
        {
            db.Entry(shortLink).State = EntityState.Detached;
            switch (violation.ConstraintName)
            {
                case CodeUniqueIndex:
                    logger.LogWarning("Short code {Code} collided with an existing row.", shortLink.Code);
                    return TryAddShortLinkResult.CodeTaken;
                case OriginalLinkUniqueIndex:
                    logger.LogWarning(
                        "Original link was inserted concurrently while saving short code {Code}.",
                        shortLink.Code);
                    return TryAddShortLinkResult.OriginalLinkTaken;
                default:
                    logger.LogError(
                        exception,
                        "Failed to save short link {Code}: unexpected unique constraint {ConstraintName}.",
                        shortLink.Code,
                        violation.ConstraintName);
                    throw;
            }
        }
    }

    private static PostgresException? FindUniqueViolation(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
                return postgres;
        }

        return null;
    }
}
