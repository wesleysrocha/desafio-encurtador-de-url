using EncurtadorUrl.Api.Tests;
using System.ComponentModel.DataAnnotations;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UrlShortener.Api.Domain;
using UrlShortener.Api.Repositories;
using UrlShortener.Api.Resources;
using UrlShortener.Api.Services;
using Xunit;
using XunitAssert = Xunit.Assert;

namespace EncurtadorUrl.Api.Tests;

public class ShortUrlServiceTests
{
    private static ShortUrlService CreateService(Mock<IShortUrlRepository> repoMock)
    {
        var inMemory = new Dictionary<string, string?>
        {
            ["Shortener:BaseUrl"] = "http://localhost:8080",
            ["Shortener:GeneratedCodeLength"] = "4" // small for tests
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
        var logger = new LoggerFactory().CreateLogger<ShortUrlService>();
        return new ShortUrlService(repoMock.Object, config, logger);
    }

    [Fact]
    public async Task CreateAsync_WhenExpirationNotProvided_SetsDefaultPlus5Minutes()
    {
        // Arrange
        var repo = new Mock<IShortUrlRepository>();
        repo.Setup(r => r.IdExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        ShortUrl? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<ShortUrl>(), It.IsAny<CancellationToken>()))
            .Callback<ShortUrl, CancellationToken>((s, ct) => captured = s)
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var svc = CreateService(repo);

        var request = new CreateShortUrlRequest
        {
            OriginalUrl = "http://example.com",
            CustomAlias = null,
            ExpirationDate = null
        };

        // Act
        var res = await svc.CreateAsync(request, CancellationToken.None);

        // Assert - entity captured and expiration approx +5 minutes
        Assert.NotNull(captured);
        var diff = captured!.ExpirationDate - DateTimeOffset.UtcNow;
        Assert.InRange(diff.TotalMinutes, 4.0, 6.0);
        Assert.Equal(request.OriginalUrl, captured.OriginalUrl);
    }

    [Fact]
    public async Task CreateAsync_WithCustomAlias_UsesProvidedAlias()
    {
        // Arrange
        var repo = new Mock<IShortUrlRepository>();
        repo.Setup(r => r.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        ShortUrl? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<ShortUrl>(), It.IsAny<CancellationToken>()))
            .Callback<ShortUrl, CancellationToken>((s, ct) => captured = s)
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var svc = CreateService(repo);

        var request = new CreateShortUrlRequest
        {
            OriginalUrl = "http://example.com",
            CustomAlias = "meu-alias",
            ExpirationDate = null
        };

        // Act
        var res = await svc.CreateAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal("meu-alias", captured!.Code);
        Assert.Equal(request.OriginalUrl, captured.OriginalUrl);
    }

    [Fact]
    public async Task CreateAsync_InvalidUrl_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<IShortUrlRepository>();
        var svc = CreateService(repo);

        var request = new CreateShortUrlRequest
        {
            OriginalUrl = "notaurl", // invalid
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => svc.CreateAsync(request, CancellationToken.None));
    }
}