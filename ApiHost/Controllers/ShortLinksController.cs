using ApiHost.Contracts;
using ApiHost.Options;
using Application.ShortLinks.Create;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ApiHost.Controllers;

[ApiController]
[Route("links")]
public sealed class ShortLinksController(
    CreateShortLinkHandler handler,
    IOptions<ShortLinkOptions> options) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("create-short-link")]
    [ProducesResponseType(typeof(CreateShortLinkResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CreateShortLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(InvalidOriginalLinkResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CreateShortLinkResponse>> Create(
        [FromBody] CreateShortLinkRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateShortLinkCommand(request?.OriginalLink),
            cancellationToken);

        return result switch
        {
            CreateShortLinkResult.Created created => Created(
                BuildResponse(created.Code, created.OriginalLink)),
            CreateShortLinkResult.AlreadyExists existing => Ok(
                BuildResponse(existing.Code, existing.OriginalLink)),
            CreateShortLinkResult.Invalid invalid => BadRequest(
                new InvalidOriginalLinkResponse { Error = invalid.Message }),
            _ => throw new InvalidOperationException($"Unexpected result {result.GetType().Name}.")
        };
    }

    private CreatedResult Created(CreateShortLinkResponse response) =>
        Created(response.ShortUrl, response);

    private CreateShortLinkResponse BuildResponse(string code, string originalLink)
    {
        var configured = options.Value.PublicBaseUrl;
        var baseUrl = string.IsNullOrWhiteSpace(configured)
            ? $"{Request.Scheme}://{Request.Host}"
            : configured.TrimEnd('/');

        return new CreateShortLinkResponse
        {
            Code = code,
            OriginalLink = originalLink,
            ShortUrl = $"{baseUrl}/{code}"
        };
    }
}
