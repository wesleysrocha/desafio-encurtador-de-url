using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UrlShortener.Api.Controllers;
using UrlShortener.Api.Domain;
using UrlShortener.Api.Resources;
using UrlShortener.Api.Services;
using Xunit;

namespace EncurtadorUrl.Api.Tests.Controllers;

public class UrlsControllerTests
{
    private static ShortUrlResponse SampleResponse(string id = "abc12") => new()
    {
        Id = id,
        CustomAlias = "alias01",
        ShortUrl = $"http://localhost:8080/alias01",
        OriginalUrl = "https://example.com",
        CreatedAt = DateTimeOffset.UtcNow,
        ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(5),
        ClickCount = 0
    };
    private static UrlsController CreateController(Mock<IShortUrlService> svcMock)
    {
        var logger = new LoggerFactory().CreateLogger<UrlsController>();
        return new UrlsController(svcMock.Object);
    }

    [Fact(DisplayName = "GET /ID encontrado com sucesso obtem detalhe")]
    public async Task Create_ReturnsCreated_WhenServiceReturnsResponse()
    {
        var svc = new Mock<IShortUrlService>();

        var response = new ShortUrlResponse
        {
            Id = "abc1",
            CustomAlias = "code",
            OriginalUrl = "http://example.com",
            ShortUrl = "http://localhost/code",
            CreatedAt = DateTime.Now
        };

        svc.Setup(s => s.CreateAsync(It.IsAny<CreateShortUrlRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(response);

        var ctrl = CreateController(svc);
        var req = new CreateShortUrlRequest { OriginalUrl = "http://example.com" };

        var result = await ctrl.Create(req, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(UrlsController.GetDetails), created.ActionName);
        Assert.Equal(response, created.Value);
    }

    [Fact(DisplayName = "GET /ID não encontra nulo")]
    public async Task Create_ServiceReturnsNull_ThrowsNullReferenceException()
    {
        var svc = new Mock<IShortUrlService>();
        svc.Setup(s => s.CreateAsync(It.IsAny<CreateShortUrlRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((ShortUrlResponse?)null);

        var ctrl = CreateController(svc);

        var req = new CreateShortUrlRequest { OriginalUrl = "http://example.com" };

        await Assert.ThrowsAsync<NullReferenceException>(() => ctrl.Create(req, CancellationToken.None));
    }

    //[Fact]
    //public async Task Create_InvalidModel_ReturnsBadRequest()
    //{
    //    var svc = new Mock<IShortUrlService>();
    //    var ctrl = CreateController(svc);

    //   ctrl.ModelState.AddModelError("OriginalUrl", "Required");

    //    var req = new CreateShortUrlRequest { OriginalUrl = "" };

    //    var result = await ctrl.Create(req, CancellationToken.None);

    //    Assert.IsType<BadRequestObjectResult>(result.Result);
    //}




    //[Fact]
    //public async Task GetDetails_NotFound_ReturnsNotFound()
    //{
    //    var svc = new Mock<IShortUrlService>();
    //    var response = new ShortUrlResponse
    //    {
    //        Id = "missing",
    //        CustomAlias = "code",
    //        OriginalUrl = "http://example.com",
    //        ShortUrl = "http://localhost/code",
    //        CreatedAt = DateTime.Now
    //    };

    //    var ctrl = CreateController(svc);

    //    var result = await ctrl.GetDetails("missing", CancellationToken.None);

    //    Assert.IsType<NotFoundResult>(result.Result);
    //}

    //    [Fact]
    //    public async Task GetDetails_Expired_ReturnsGone()
    //    {
    //        var svc = new Mock<ShortUrlService>();
    //        svc.Setup(s => s.GetDetailsAsync("expired", It.IsAny<CancellationToken>()))
    //           .ThrowsAsync(new ExpiredException("expired"));

    //        var ctrl = CreateController(svc);

    //        var result = await ctrl.GetDetails("expired", CancellationToken.None);

    //        Assert.IsType<StatusCodeResult>(result.Result);
    //        Assert.Equal(StatusCodes.Status410Gone, ((StatusCodeResult)result.Result).StatusCode);
    //    }

    //    [Fact]
    //    public async Task List_ReturnsOk_WithPagedResult()
    //    {
    //        var svc = new Mock<ShortUrlService>();
    //        var list = new List<ShortUrlResponse>
    //        {
    //            new ShortUrlResponse { Id = "a", Code = "c1", OriginalUrl = "http://a" },
    //            new ShortUrlResponse { Id = "b", Code = "c2", OriginalUrl = "http://b" }
    //        };

    //        svc.Setup(s => s.ListAsync(1, 50, It.IsAny<CancellationToken>()))
    //           .ReturnsAsync(list);

    //        var ctrl = CreateController(svc);

    //        var result = await ctrl.List(page: 1, pageSize: 50, ct: CancellationToken.None);

    //        var ok = Assert.IsType<OkObjectResult>(result.Result);
    //        Assert.IsAssignableFrom<IEnumerable<ShortUrlResponse>>(ok.Value);
    //    }

    //    [Fact]
    //    public async Task List_InvalidParameters_ReturnsBadRequest()
    //    {
    //        var svc = new Mock<ShortUrlService>();
    //        svc.Setup(s => s.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
    //           .ThrowsAsync(new ValidationException("invalid"));

    //        var ctrl = CreateController(svc);

    //        var result = await ctrl.List(page: -1, pageSize: 0, ct: CancellationToken.None);

    //        Assert.IsType<BadRequestObjectResult>(result.Result);
    //    }

    //    [Fact]
    //    public async Task Delete_ReturnsNoContent_WhenServiceSucceeds()
    //    {
    //        var svc = new Mock<ShortUrlService>();
    //        svc.Setup(s => s.DeleteAsync("id1", It.IsAny<CancellationToken>()))
    //           .Returns(Task.CompletedTask);

    //        var ctrl = CreateController(svc);

    //        var result = await ctrl.Delete("id1", CancellationToken.None);

    //        Assert.IsType<NoContentResult>(result);
    //    }

    //    [Fact]
    //    public async Task Delete_NotFound_ReturnsNotFound()
    //    {
    //        var svc = new Mock<ShortUrlService>();
    //        svc.Setup(s => s.DeleteAsync("missing", It.IsAny<CancellationToken>()))
    //           .ThrowsAsync(new NotFoundException("not found"));

    //        var ctrl = CreateController(svc);

    //        var result = await ctrl.Delete("missing", CancellationToken.None);

    //        Assert.IsType<NotFoundResult>(result);
    //    }

    //    [Fact]
    //    public async Task Delete_InvalidId_ReturnsBadRequest()
    //    {
    //        var svc = new Mock<ShortUrlService>();
    //        svc.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    //           .ThrowsAsync(new ValidationException("invalid"));

    //        var ctrl = CreateController(svc);

    //        var result = await ctrl.Delete("", CancellationToken.None);

    //        Assert.IsType<BadRequestObjectResult>(result);
    //    }




    //--
    //using FluentAssertions;
    //using Microsoft.AspNetCore.Mvc;
    //using Moq;
    //using UrlShortener.Api.Controllers;
    //using UrlShortener.Api.Resources;
    //using UrlShortener.Api.Services;
    //using Xunit;

    //namespace UrlShortener.Tests.Controllers;

    //public sealed class UrlsControllerTests
    //{


    [Fact(DisplayName = "POST /ID criado com sucesso")]
    public async Task Create_WhenSuccess_ShouldReturn201CreatedAtAction()
    {
        var service = new Mock<IShortUrlService>();
        var request = new CreateShortUrlRequest { OriginalUrl = "https://example.com" };
        var created = SampleResponse("id-123");

        service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
               .ReturnsAsync(created);

        var controller = new UrlsController(service.Object);

        var result = await controller.Create(request, CancellationToken.None);

        var createdAt = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAt.ActionName.Should().Be(nameof(UrlsController.GetDetails));
        createdAt.RouteValues.Should().ContainKey("id");
        createdAt.RouteValues!["id"].Should().Be(created.Id);

        createdAt.Value.Should().BeEquivalentTo(created);
    }

    [Fact(DisplayName = "GET /ID encontrado com sucesso obtem detalhe")]
    public async Task GetDetails_WhenSuccess_ShouldReturn200Ok()
    {
        var service = new Mock<IShortUrlService>();
        var response = SampleResponse("abc12");

        service.Setup(s => s.GetDetailsAsync("abc12", It.IsAny<CancellationToken>()))
               .ReturnsAsync(response);

        var controller = new UrlsController(service.Object);

        var result = await controller.GetDetails("abc12", CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(response);
    }

    [Fact(DisplayName = "GET todas as URLS encontradas com sucesso")]
    public async Task List_WhenSuccess_ShouldReturn200OkWithList()
    {
        var service = new Mock<IShortUrlService>();
        var list = new List<ShortUrlResponse> { SampleResponse("1"), SampleResponse("2") };

        service.Setup(s => s.ListAsync(1, 50, It.IsAny<CancellationToken>()))
               .ReturnsAsync(list);

        var controller = new UrlsController(service.Object);

        var result = await controller.List(page: 1, pageSize: 50, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(list);
    }

    [Fact(DisplayName = "DELETE /id deletado com sucesso")]
    public async Task Delete_WhenSuccess_ShouldReturn204NoContent()
    {
        var service = new Mock<IShortUrlService>();

        service.Setup(s => s.DeleteAsync("abc12", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var controller = new UrlsController(service.Object);

        var result = await controller.Delete("abc12", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetDetails_WhenServiceThrowsValidation_ShouldBubbleException(string? id)
    {
        var service = new Mock<IShortUrlService>();
        service.Setup(s => s.GetDetailsAsync(id!, It.IsAny<CancellationToken>()))
               .ThrowsAsync(new ValidationException("id é obrigatório."));

        var controller = new UrlsController(service.Object);

        var act = async () => await controller.GetDetails(id!, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
