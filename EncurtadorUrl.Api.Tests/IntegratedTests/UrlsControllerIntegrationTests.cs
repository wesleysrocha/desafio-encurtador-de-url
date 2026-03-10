using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using UrlShortener.Api.Resources;
using Xunit;

namespace EncurtadorUrl.Api.Tests.IntegratedTests;

public sealed class UrlsControllerIntegrationTests : IClassFixture<SetupIntegratedTest>
{
    private readonly SetupIntegratedTest _factory;

    public UrlsControllerIntegrationTests(SetupIntegratedTest factory) => _factory = factory;

    [Fact(DisplayName = "POST cria short URL")]
    public async Task Post_CreateUrl_ReturnsCreated_WithShortUrl()
    {
        var client = _factory.CreateClient();

        var payload = new CreateShortUrlRequest
        {
            OriginalUrl = "http://example.com"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/urls")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-API-Key", "test-key");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ShortUrlResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Id));
        Assert.False(string.IsNullOrWhiteSpace(body.ShortUrl));
        Assert.Equal("http://example.com", body.OriginalUrl);
    }

    [Fact(DisplayName = "GET URLS/ID retorna detalhes")]
    public async Task GetDetails_AfterCreate_Returns200_WithSameData()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createPayload = new CreateShortUrlRequest
        {
            OriginalUrl = "http://example.com"
        };

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/urls")
        {
            Content = JsonContent.Create(createPayload)
        };
        createRequest.Headers.Add("X-API-Key", "test-key");

        var createResponse = await client.SendAsync(createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ShortUrlResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeNullOrWhiteSpace();
        created.OriginalUrl.Should().Be("http://example.com");

        var getResponse = await client.GetAsync($"/v1/urls/{created.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var details = await getResponse.Content.ReadFromJsonAsync<ShortUrlResponse>();
        details.Should().NotBeNull();
        details!.Id.Should().Be(created.Id);
        details.OriginalUrl.Should().Be(created.OriginalUrl);
        details.ShortUrl.Should().NotBeNullOrWhiteSpace();
        details.CreatedAt.Should().BeCloseTo(created.CreatedAt, precision: TimeSpan.FromSeconds(5));
    }
}