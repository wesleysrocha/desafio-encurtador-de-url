namespace UrlShortener.Api.Resources;

public sealed class ShortUrlResponse
{
    public required string Id { get; init; }
    public required string CustomAlias { get; init; }
    public required string ShortUrl { get; init; }
    public required string OriginalUrl { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpirationDate { get; init; }
    public long ClickCount { get; init; }
}