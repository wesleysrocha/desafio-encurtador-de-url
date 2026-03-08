namespace UrlShortener.Api.Domain;

public sealed class ShortUrl
{
    private ShortUrl() { }

    public ShortUrl(string code, string originalUrl, DateTimeOffset? expirationDate)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        OriginalUrl = originalUrl ?? throw new ArgumentNullException(nameof(originalUrl));
        ExpirationDate = expirationDate;
        CreatedAt = DateTimeOffset.UtcNow;
        ClickCount = 0;
    }

    public long Id { get; private set; }

    public string Code { get; private set; } = default!;

    public string OriginalUrl { get; private set; } = default!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ExpirationDate { get; private set; }

    public long ClickCount { get; private set; }

    public bool IsExpired(DateTimeOffset nowUtc)
        => ExpirationDate is not null && ExpirationDate <= nowUtc;

    public void RegisterClick() => ClickCount++;

    public void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code não pode ser vazio.", nameof(code));

        Code = code;
    }
}