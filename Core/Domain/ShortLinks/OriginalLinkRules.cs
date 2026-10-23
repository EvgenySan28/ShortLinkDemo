using System.Diagnostics.CodeAnalysis;

namespace Domain.ShortLinks;

public static class OriginalLinkRules
{
    public const int MaxLength = 2048;

    public static bool TryNormalize(
        string? value,
        [NotNullWhen(true)] out string? normalized,
        [NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            normalized = null;
            error = "Original link is required.";
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            normalized = null;
            error = $"Original link must be at most {MaxLength} characters.";
            return false;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            normalized = null;
            error = "Original link must be an absolute http or https URL.";
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            normalized = null;
            error = "Original link must not include user info.";
            return false;
        }

        normalized = trimmed;
        error = null;
        return true;
    }
}
