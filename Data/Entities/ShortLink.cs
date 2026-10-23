namespace Data.Entities;

public class ShortLink
{
    public Guid Id { get; set; }
    public required string OriginalLink { get; set; }
    public required string Code { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long RedirectCount { get; set; }
}
