using Application.Abstractions;
using Application.ShortLinks.Create;
using Domain.ShortLinks;
using Microsoft.Extensions.Time.Testing;

namespace Application.Tests;

public sealed class CreateShortLinkHandlerTests
{
    private const string OriginalLink = "https://example.com/a";
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_NewUrl_ReturnsCreated()
    {
        var (repository, codeGenerator) = CreateMocks();

        repository
            .Setup(r => r.FindCodeByOriginalLink(OriginalLink, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        repository
            .Setup(r => r.TryAdd(It.IsAny<ShortLink>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TryAddShortLinkResult.Added);
        codeGenerator.Setup(g => g.Generate()).Returns("newCode");

        var result = await CreateSut(repository, codeGenerator)
            .Handle(new CreateShortLinkCommand($" {OriginalLink} "), CancellationToken.None);

        var created = Assert.IsType<CreateShortLinkResult.Created>(result);
        Assert.Equal("newCode", created.Code);
        Assert.Equal(OriginalLink, created.OriginalLink);

        VerifyAll(repository, codeGenerator);
    }

    [Fact]
    public async Task Handle_ExistingUrl_ReturnsAlreadyExists()
    {
        const string existingLink = "https://example.com/docs";
        const string existingCode = "abc1234";
        var (repository, codeGenerator) = CreateMocks();

        repository
            .Setup(r => r.FindCodeByOriginalLink(existingLink, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCode);

        var result = await CreateSut(repository, codeGenerator)
            .Handle(new CreateShortLinkCommand($"  {existingLink}  "), CancellationToken.None);

        var alreadyExists = Assert.IsType<CreateShortLinkResult.AlreadyExists>(result);
        Assert.Equal(existingCode, alreadyExists.Code);
        Assert.Equal(existingLink, alreadyExists.OriginalLink);

        VerifyAll(repository, codeGenerator);
    }

    [Fact]
    public async Task Handle_CodeTaken_RetriesThenCreates()
    {
        var (repository, codeGenerator) = CreateMocks();

        repository
            .Setup(r => r.FindCodeByOriginalLink(OriginalLink, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        repository
            .SetupSequence(r => r.TryAdd(It.IsAny<ShortLink>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TryAddShortLinkResult.CodeTaken)
            .ReturnsAsync(TryAddShortLinkResult.Added);
        codeGenerator.SetupSequence(g => g.Generate()).Returns("taken").Returns("fresh");

        var result = await CreateSut(repository, codeGenerator)
            .Handle(new CreateShortLinkCommand(OriginalLink), CancellationToken.None);

        var created = Assert.IsType<CreateShortLinkResult.Created>(result);
        Assert.Equal("fresh", created.Code);

        repository.Verify(r => r.TryAdd(It.IsAny<ShortLink>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        codeGenerator.Verify(g => g.Generate(), Times.Exactly(2));
        VerifyAll(repository, codeGenerator);
    }

    [Fact]
    public async Task Handle_OriginalLinkTaken_ReturnsWinner()
    {
        const string winnerCode = "winner1";
        var (repository, codeGenerator) = CreateMocks();

        repository
            .SetupSequence(r => r.FindCodeByOriginalLink(OriginalLink, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null)
            .ReturnsAsync(winnerCode);
        repository
            .Setup(r => r.TryAdd(It.IsAny<ShortLink>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TryAddShortLinkResult.OriginalLinkTaken);
        codeGenerator.Setup(g => g.Generate()).Returns("loser11");

        var result = await CreateSut(repository, codeGenerator)
            .Handle(new CreateShortLinkCommand(OriginalLink), CancellationToken.None);

        var alreadyExists = Assert.IsType<CreateShortLinkResult.AlreadyExists>(result);
        Assert.Equal(winnerCode, alreadyExists.Code);
        Assert.Equal(OriginalLink, alreadyExists.OriginalLink);

        repository.Verify(r => r.FindCodeByOriginalLink(OriginalLink, It.IsAny<CancellationToken>()), Times.Exactly(2));
        VerifyAll(repository, codeGenerator);
    }

    [Fact]
    public async Task Handle_OriginalLinkTakenAndWinnerMissing_Throws()
    {
        var (repository, codeGenerator) = CreateMocks();

        repository
            .Setup(r => r.FindCodeByOriginalLink(OriginalLink, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        repository
            .Setup(r => r.TryAdd(It.IsAny<ShortLink>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TryAddShortLinkResult.OriginalLinkTaken);
        codeGenerator.Setup(g => g.Generate()).Returns("loser11");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut(repository, codeGenerator)
                .Handle(new CreateShortLinkCommand(OriginalLink), CancellationToken.None));

        Assert.Equal("A conflicting short link was not found.", exception.Message);
        repository.Verify(r => r.FindCodeByOriginalLink(OriginalLink, It.IsAny<CancellationToken>()), Times.Exactly(2));
        VerifyAll(repository, codeGenerator);
    }

    [Fact]
    public async Task Handle_AllCodesTaken_Throws()
    {
        var (repository, codeGenerator) = CreateMocks();

        repository
            .Setup(r => r.FindCodeByOriginalLink(OriginalLink, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        repository
            .Setup(r => r.TryAdd(It.IsAny<ShortLink>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TryAddShortLinkResult.CodeTaken);
        codeGenerator.Setup(g => g.Generate()).Returns("taken");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut(repository, codeGenerator)
                .Handle(new CreateShortLinkCommand(OriginalLink), CancellationToken.None));

        Assert.Equal("Failed to allocate a unique short code.", exception.Message);
        repository.Verify(
            r => r.TryAdd(It.IsAny<ShortLink>(), It.IsAny<CancellationToken>()),
            Times.Exactly(CreateShortLinkHandler.MaxCodeAttempts));
        codeGenerator.Verify(g => g.Generate(), Times.Exactly(CreateShortLinkHandler.MaxCodeAttempts));
        VerifyAll(repository, codeGenerator);
    }

    [Theory]
    [InlineData(null, "Original link is required.")]
    [InlineData("", "Original link is required.")]
    [InlineData("   ", "Original link is required.")]
    [InlineData("example.com", "Original link must be an absolute http or https URL.")]
    [InlineData("ftp://example.com/a", "Original link must be an absolute http or https URL.")]
    [InlineData("https://user:pass@example.com/a", "Original link must not include user info.")]
    public async Task Handle_InvalidUrl_ReturnsInvalid(string? originalLink, string message)
    {
        var (repository, codeGenerator) = CreateMocks();

        var result = await CreateSut(repository, codeGenerator)
            .Handle(new CreateShortLinkCommand(originalLink), CancellationToken.None);

        var invalid = Assert.IsType<CreateShortLinkResult.Invalid>(result);
        Assert.Equal(message, invalid.Message);

        VerifyAll(repository, codeGenerator);
    }

    [Fact]
    public async Task Handle_UrlTooLong_ReturnsInvalid()
    {
        var (repository, codeGenerator) = CreateMocks();
        var originalLink = "https://example.com/" + new string('a', OriginalLinkRules.MaxLength);

        var result = await CreateSut(repository, codeGenerator)
            .Handle(new CreateShortLinkCommand(originalLink), CancellationToken.None);

        var invalid = Assert.IsType<CreateShortLinkResult.Invalid>(result);
        Assert.Equal($"Original link must be at most {OriginalLinkRules.MaxLength} characters.", invalid.Message);

        VerifyAll(repository, codeGenerator);
    }

    [Fact]
    public async Task Handle_UrlAtMaxLength_ReturnsCreated()
    {
        var (repository, codeGenerator) = CreateMocks();
        var prefix = "https://example.com/";
        var originalLink = prefix + new string('a', OriginalLinkRules.MaxLength - prefix.Length);

        repository
            .Setup(r => r.FindCodeByOriginalLink(originalLink, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        repository
            .Setup(r => r.TryAdd(It.IsAny<ShortLink>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TryAddShortLinkResult.Added);
        codeGenerator.Setup(g => g.Generate()).Returns("maxCode");

        var result = await CreateSut(repository, codeGenerator)
            .Handle(new CreateShortLinkCommand(originalLink), CancellationToken.None);

        var created = Assert.IsType<CreateShortLinkResult.Created>(result);
        Assert.Equal(originalLink, created.OriginalLink);
        Assert.Equal(OriginalLinkRules.MaxLength, created.OriginalLink.Length);

        VerifyAll(repository, codeGenerator);
    }

    private static (Mock<IShortLinkRepository> Repository, Mock<ILinkCodeGenerator> CodeGenerator) CreateMocks()
        => (
            new Mock<IShortLinkRepository>(MockBehavior.Strict),
            new Mock<ILinkCodeGenerator>(MockBehavior.Strict));

    private static CreateShortLinkHandler CreateSut(
        Mock<IShortLinkRepository> repository,
        Mock<ILinkCodeGenerator> codeGenerator)
        => new(repository.Object, codeGenerator.Object, new FakeTimeProvider(CreatedAt));

    private static void VerifyAll(
        Mock<IShortLinkRepository> repository,
        Mock<ILinkCodeGenerator> codeGenerator)
    {
        repository.VerifyAll();
        codeGenerator.VerifyAll();
    }
}
