namespace Data.Options;

public sealed class RedirectCacheOptions
{
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);
}
