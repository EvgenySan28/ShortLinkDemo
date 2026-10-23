using Application.Abstractions;
using Domain.ShortLinks;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Data.Persistence;

internal sealed class ShortLinkRepository(ShortLinkDbContext db) : IShortLinkRepository
{
    private const string CodeUniqueIndex = "IX_ShortLinks_Code";
    private const string OriginalLinkUniqueIndex = "IX_ShortLinks_OriginalLink";

    public Task<ShortLink?> FindByOriginalLink(string originalLink, CancellationToken cancellationToken)
    {
        return db.ShortLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(link => link.OriginalLink == originalLink, cancellationToken);
    }

    public async Task<TryAddShortLinkResult> TryAdd(ShortLink shortLink, CancellationToken cancellationToken)
    {
        db.ShortLinks.Add(shortLink);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return TryAddShortLinkResult.Added;
        }
        catch (DbUpdateException exception) when (FindUniqueViolation(exception) is not null)
        {
            db.Entry(shortLink).State = EntityState.Detached;
            switch (FindUniqueViolation(exception)!.ConstraintName)
            {
                case CodeUniqueIndex:
                    return TryAddShortLinkResult.CodeTaken;
                case OriginalLinkUniqueIndex:
                    return TryAddShortLinkResult.OriginalLinkTaken;
                default:
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
