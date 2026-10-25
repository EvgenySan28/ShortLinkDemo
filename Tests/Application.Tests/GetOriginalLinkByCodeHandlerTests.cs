using Application.Abstractions;
using Application.ShortLinks.GetOriginalLink;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests;

public sealed class GetOriginalLinkByCodeHandlerTests
{
    private const string Code = "Ab12Cd34";
    private const string OriginalLink = "https://example.com/docs";

    [Fact]
    public async Task Handle_KnownCode_ReturnsFound()
    {
        var repository = new Mock<IShortLinkRepository>(MockBehavior.Strict);
        repository
            .Setup(r => r.FindOriginalLinkByCode(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OriginalLink);
        repository
            .Setup(r => r.IncrementRedirectCount(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateSut(repository)
            .Handle(new GetOriginalLinkByCodeCommand(Code), CancellationToken.None);

        var found = Assert.IsType<GetOriginalLinkByCodeResult.Found>(result);
        Assert.Equal(OriginalLink, found.OriginalLink);
        repository.VerifyAll();
    }

    [Fact]
    public async Task Handle_UnknownCode_ReturnsNotFound()
    {
        var repository = new Mock<IShortLinkRepository>(MockBehavior.Strict);
        repository
            .Setup(r => r.FindOriginalLinkByCode(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await CreateSut(repository)
            .Handle(new GetOriginalLinkByCodeCommand(Code), CancellationToken.None);

        Assert.IsType<GetOriginalLinkByCodeResult.NotFound>(result);
        repository.VerifyAll();
    }

    [Fact]
    public async Task Handle_KnownCodeMissingAtIncrement_ReturnsFound()
    {
        var repository = new Mock<IShortLinkRepository>(MockBehavior.Strict);
        repository
            .Setup(r => r.FindOriginalLinkByCode(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OriginalLink);
        repository
            .Setup(r => r.IncrementRedirectCount(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateSut(repository)
            .Handle(new GetOriginalLinkByCodeCommand(Code), CancellationToken.None);

        var found = Assert.IsType<GetOriginalLinkByCodeResult.Found>(result);
        Assert.Equal(OriginalLink, found.OriginalLink);
        repository.VerifyAll();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyCode_ReturnsNotFound(string? code)
    {
        var repository = new Mock<IShortLinkRepository>(MockBehavior.Strict);

        var result = await CreateSut(repository)
            .Handle(new GetOriginalLinkByCodeCommand(code), CancellationToken.None);

        Assert.IsType<GetOriginalLinkByCodeResult.NotFound>(result);
        repository.VerifyAll();
    }

    private static GetOriginalLinkByCodeHandler CreateSut(Mock<IShortLinkRepository> repository)
        => new(repository.Object, NullLogger<GetOriginalLinkByCodeHandler>.Instance);
}
