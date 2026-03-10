using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UrlShortener.Api.Domain;
using UrlShortener.Api.Repositories;
using UrlShortener.Api.Resources;
using UrlShortener.Api.Services;
using ValidationException = UrlShortener.Api.Services.ValidationException;

namespace EncurtadorUrl.Api.Tests;

public class ShortUrlServiceTests
{
    private static ShortUrlService CreateService(Mock<IShortUrlRepository> repoMock)
    {
        var inMemory = new Dictionary<string, string?>
        {
            ["Shortener:BaseUrl"] = "http://localhost:8080"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
        var logger = new LoggerFactory().CreateLogger<ShortUrlService>();
        return new ShortUrlService(repoMock.Object, config, logger);
    }

    [Fact(DisplayName = "ExpirationDate não informado, setta para 5 minutos por padrão")]
    public async Task CreateAsync_WhenExpirationNotProvided_SetsDefaultPlus5Minutes()
    {
        // Arrange
        var repo = new Mock<IShortUrlRepository>();
        repo.Setup(r => r.IdExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        ShortUrl? resultUrl = null;
        repo.Setup(r => r.AddAsync(It.IsAny<ShortUrl>(), It.IsAny<CancellationToken>()))
            .Callback<ShortUrl, CancellationToken>((s, ct) => resultUrl = s)
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

        // Assert 
        Assert.NotNull(resultUrl);
        Assert.NotNull(resultUrl!.ExpirationDate);
        var diff = resultUrl.ExpirationDate.Value - DateTimeOffset.UtcNow;
        Assert.InRange(diff.TotalMinutes, 4.0, 6.0);
        Assert.Equal(request.OriginalUrl, resultUrl.OriginalUrl);
    }

    [Fact(DisplayName = "Permitir que cliente envie um alias customizado")]
    public async Task CreateAsync_WithCustomAlias_UsesProvidedAlias()
    {
        // Arrange
        var repo = new Mock<IShortUrlRepository>();
        repo.Setup(r => r.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        ShortUrl? resultUrl = null;
        repo.Setup(r => r.AddAsync(It.IsAny<ShortUrl>(), It.IsAny<CancellationToken>()))
            .Callback<ShortUrl, CancellationToken>((s, ct) => resultUrl = s)
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
        Assert.NotNull(resultUrl);
        Assert.Equal("meu-alias", resultUrl!.Code);
        Assert.Equal(request.OriginalUrl, resultUrl.OriginalUrl);
    }

    [Fact(DisplayName = "OriginalUrl formato invalido retorna excessão")]
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

    [Fact(DisplayName = "OriginalUrl nulo retorna excessão")]
    public async Task CreateAsync_OriginalUrlNull_ThrowsValidationException()
    {
        var repo = new Mock<IShortUrlRepository>();
        var svc = CreateService(repo);

        var req = new CreateShortUrlRequest { OriginalUrl = null! };

        await Assert.ThrowsAsync<ValidationException>(() => svc.CreateAsync(req, CancellationToken.None));
    }

    [Fact(DisplayName = "OriginalUrl vazia retorna excessão")]
    public async Task CreateAsync_OriginalUrlWhitespace_ThrowsValidationException()
    {
        var repo = new Mock<IShortUrlRepository>();
        var svc = CreateService(repo);

        var req = new CreateShortUrlRequest { OriginalUrl = "   " };

        await Assert.ThrowsAsync<ValidationException>(() => svc.CreateAsync(req, CancellationToken.None));
    }

    [Fact(DisplayName = "Evitar colisões (duas URLs diferentes não podem ter o mesmo id")]
    public async Task CreateAsync_RetriesWhenIdAlreadyExists()
    {
        var repo = new Mock<IShortUrlRepository>();

        int idCallCount = 0;
        repo.Setup(r => r.IdExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((s, ct) =>
                Task.FromResult(Interlocked.Increment(ref idCallCount) == 1));

        repo.Setup(r => r.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        ShortUrl? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<ShortUrl>(), It.IsAny<CancellationToken>()))
            .Callback<ShortUrl, CancellationToken>((s, ct) => captured = s)
            .Returns(Task.CompletedTask);

        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = CreateService(repo);

        var req = new CreateShortUrlRequest { OriginalUrl = "http://example.com" };

        var res = await svc.CreateAsync(req, CancellationToken.None);

        Assert.NotNull(captured);

        repo.Verify(r => r.IdExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
        repo.Verify(r => r.AddAsync(It.IsAny<ShortUrl>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(captured!.OriginalUrl, req.OriginalUrl);
    }

    [Fact(DisplayName = "Contabilizar clickCount")]
    public async Task ResolveAndCountClickAsync_ReturnsOriginalUrlAndIncrementsClick()
    {
        var repo = new Mock<IShortUrlRepository>();
        var entity = new ShortUrl(id: "id1", code: "code1", originalUrl: "http://example.com", expirationDate: null);

        repo.Setup(r => r.GetByIdAsync("id1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = CreateService(repo);

        var result = await svc.ResolveAndCountClickAsync("id1", CancellationToken.None);

        Assert.Equal("http://example.com", result);
        Assert.Equal(1, entity.ClickCount);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName ="ID não encontrado, retorna exception")]
    public async Task ResolveAndCountClickAsync_IdNotFound_ThrowsNotFoundException()
    {
        var repo = new Mock<IShortUrlRepository>();
        repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShortUrl?)null);

        var svc = CreateService(repo);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.ResolveAndCountClickAsync("missing", CancellationToken.None));
    }

    [Fact(DisplayName = "Não retornar URLs com expirationDate expiradas")]
    public async Task ResolveAndCountClickAsync_Expired_ThrowsExpiredException()
    {
        var repo = new Mock<IShortUrlRepository>();
        var expired = new ShortUrl(id: "idExpired", code: "code", originalUrl: "http://example.com", expirationDate: DateTimeOffset.UtcNow.AddMinutes(-10));

        repo.Setup(r => r.GetByIdAsync("idExpired", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expired);

        var svc = CreateService(repo);

        await Assert.ThrowsAsync<ExpiredException>(() => svc.ResolveAndCountClickAsync("idExpired", CancellationToken.None));
    }
}