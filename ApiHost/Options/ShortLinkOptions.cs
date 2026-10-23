using System.ComponentModel.DataAnnotations;

namespace ApiHost.Options;

public sealed class ShortLinkOptions
{
    [Required]
    public string? PublicBaseUrl { get; set; }
}
