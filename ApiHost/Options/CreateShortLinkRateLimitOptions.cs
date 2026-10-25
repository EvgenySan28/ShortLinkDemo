using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ApiHost.Options;

public sealed class CreateShortLinkRateLimitOptions
{
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 1;

    public TimeSpan Window { get; set; } = TimeSpan.FromSeconds(5);

    public string FormatError()
    {
        var seconds = Window.TotalSeconds;
        var amount = seconds.ToString(seconds % 1d == 0d ? "0" : "0.###", CultureInfo.InvariantCulture);
        return PermitLimit == 1
            ? $"Short links can be created once every {amount} seconds."
            : $"Short links can be created at most {PermitLimit} times every {amount} seconds.";
    }
}
