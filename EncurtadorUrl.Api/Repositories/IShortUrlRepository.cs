using UrlShortener.Api.Domain;

namespace UrlShortener.Api.Repositories;

public interface IShortUrlRepository
{
    Task<bool> IdExistsAsync(string id, CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);
    Task<ShortUrl?> GetByIdAsync(string id, CancellationToken ct);
    Task<ShortUrl?> GetByIdAsNoTrackingAsync(string id, CancellationToken ct);

    Task AddAsync(ShortUrl entity, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task<IReadOnlyList<Domain.ShortUrl>> ListAsync(int skip, int take, CancellationToken ct);
    Task DeleteAsync(ShortUrl entity, CancellationToken ct);
}