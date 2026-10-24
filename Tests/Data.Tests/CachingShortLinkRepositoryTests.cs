using System.Text;
using Application.Abstractions;
using Data.Options;
using Data.Persistence;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Data.Tests;

public sealed class CachingShortLinkRepositoryTests
{
    private const string Code = "Ab12Cd34";
    private const string OriginalLink = "https://example.com/docs";
    private static readonly TimeSpan Duration = TimeSpan.FromMinutes(5);

    [Fact]
    public async Task FindOriginalLinkByCode_CacheMiss_ReadsRepositoryAndStoresValue()
    {
        var repository = new Mock<IShortLinkRepository>(MockBehavior.Strict);
        var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
        repository
            .Setup(r => r.FindOriginalLinkByCode(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OriginalLink);
        cache
            .Setup(c => c.GetAsync(CacheKey(Code), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        cache
            .Setup(c => c.SetAsync(
                CacheKey(Code),
                It.Is<byte[]>(value => Encoding.UTF8.GetString(value) == OriginalLink),
                It.Is<DistributedCacheEntryOptions>(entry => entry.AbsoluteExpirationRelativeToNow == Duration),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateSut(repository, cache, Duration)
            .FindOriginalLinkByCode(Code, CancellationToken.None);

        Assert.Equal(OriginalLink, result);
        repository.VerifyAll();
        cache.VerifyAll();
    }

    [Fact]
    public async Task FindOriginalLinkByCode_CacheHit_DoesNotReadRepository()
    {
        var repository = new Mock<IShortLinkRepository>(MockBehavior.Strict);
        var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
        cache
            .Setup(c => c.GetAsync(CacheKey(Code), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(OriginalLink));

        var result = await CreateSut(repository, cache, Duration)
            .FindOriginalLinkByCode(Code, CancellationToken.None);

        Assert.Equal(OriginalLink, result);
        repository.VerifyAll();
        cache.VerifyAll();
    }

    [Fact]
    public async Task FindOriginalLinkByCode_UnknownCode_DoesNotStoreValue()
    {
        var repository = new Mock<IShortLinkRepository>(MockBehavior.Strict);
        var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
        repository
            .Setup(r => r.FindOriginalLinkByCode(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        cache
            .Setup(c => c.GetAsync(CacheKey(Code), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var result = await CreateSut(repository, cache, Duration)
            .FindOriginalLinkByCode(Code, CancellationToken.None);

        Assert.Null(result);
        repository.VerifyAll();
        cache.VerifyAll();
    }

    private static CachingShortLinkRepository CreateSut(
        Mock<IShortLinkRepository> repository,
        Mock<IDistributedCache> cache,
        TimeSpan duration)
    {
        return new CachingShortLinkRepository(
            repository.Object,
            cache.Object,
            OptionsFactory.Create(new RedirectCacheOptions { Duration = duration }),
            NullLogger<CachingShortLinkRepository>.Instance);
    }

    private static string CacheKey(string code) => "redirect:" + code;
}
