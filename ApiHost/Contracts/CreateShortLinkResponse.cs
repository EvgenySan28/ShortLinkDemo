namespace ApiHost.Contracts;

public sealed class CreateShortLinkResponse
{
    public string Code { get; set; } = default!;
    public string OriginalLink { get; set; } = default!;
    public string ShortUrl { get; set; } = default!;
}
