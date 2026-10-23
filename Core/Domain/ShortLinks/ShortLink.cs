namespace Domain.ShortLinks;

public sealed class ShortLink
{
    public long Id { get; private set; }
    public string OriginalLink { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }
    public long RedirectCount { get; private set; }

    private ShortLink()
    {
    }

    public static ShortLink Create(string originalLink, string code, DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalLink);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (createdAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Creation time must be UTC.", nameof(createdAtUtc));

        return new ShortLink
        {
            OriginalLink = originalLink,
            Code = code,
            CreatedAtUtc = createdAtUtc,
            RedirectCount = 0
        };
    }
}
