namespace UrlShortener.Api.Resources;

public sealed class CreateShortUrlRequest
{
    public string? OriginalUrl { get; init; }
    public DateTimeOffset? ExpirationDate { get; init; }
    public string? CustomAlias { get; init; }
}