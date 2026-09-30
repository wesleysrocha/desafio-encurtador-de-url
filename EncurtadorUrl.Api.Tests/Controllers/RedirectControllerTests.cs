using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UrlShortener.Api.Controllers;
using UrlShortener.Api.Services;
using Xunit;

namespace UrlShortener.Tests.Controllers;

public sealed class RedirectControllerTests
{
    [Fact(DisplayName = "GET /ID redireciona para a URL original")]
    public async Task RedirectToOriginal_WhenSuccess_ShouldRedirectToOriginalUrl()
    {
        var service = new Mock<IShortUrlService>();

        service.Setup(s => s.ResolveAndCountClickAsync("abc12", It.IsAny<CancellationToken>()))
               .ReturnsAsync("https://example.com");

        var controller = new RedirectController(service.Object);

        var result = await controller.RedirectToOriginal("abc12", CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be("https://example.com");
        redirect.Permanent.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RedirectToOriginal_WhenServiceThrowsValidation_ShouldBubbleException(string? id)
    {
        var service = new Mock<IShortUrlService>();

        service.Setup(s => s.ResolveAndCountClickAsync(id!, It.IsAny<CancellationToken>()))
               .ThrowsAsync(new ValidationException("ID é obrigatório."));

        var controller = new RedirectController(service.Object);

        var act = async () => await controller.RedirectToOriginal(id!, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}