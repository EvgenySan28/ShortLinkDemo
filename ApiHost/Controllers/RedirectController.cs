using Application.ShortLinks.GetOriginalLink;
using Microsoft.AspNetCore.Mvc;

namespace ApiHost.Controllers;

[ApiController]
public sealed class RedirectController(GetOriginalLinkByCodeHandler handler) : ControllerBase
{
    [HttpGet("/{code:length(8)}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedirectByCode(
        [FromRoute] string code,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetOriginalLinkByCodeCommand(code), cancellationToken);

        return result switch
        {
            GetOriginalLinkByCodeResult.Found found => Redirect(found.OriginalLink),
            GetOriginalLinkByCodeResult.NotFound => NotFound(),
            _ => throw new InvalidOperationException($"Unexpected result {result.GetType().Name}.")
        };
    }
}
