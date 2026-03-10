using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Resources;

namespace UrlShortener.Api.Services;

public interface IShortUrlService
{
    Task<ShortUrlResponse> CreateAsync(CreateShortUrlRequest request, CancellationToken ct);
    Task<ShortUrlResponse> GetDetailsAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<ShortUrlResponse>> ListAsync(int page, int pageSize, CancellationToken ct);
    Task DeleteAsync(string id, CancellationToken ct);
    Task<string> ResolveAndCountClickAsync(string id, CancellationToken ct);
}