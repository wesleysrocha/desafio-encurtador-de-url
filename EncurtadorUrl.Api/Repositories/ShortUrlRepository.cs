using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Domain;

namespace UrlShortener.Api.Repositories;

public sealed class ShortUrlRepository(AppDbContext db) : IShortUrlRepository
{
    public Task<bool> IdExistsAsync(string id, CancellationToken ct)
        => db.ShortUrls.AnyAsync(x => x.Id == id, ct);

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct)
            => db.ShortUrls.AnyAsync(x => x.Code == code, ct);
    public Task<ShortUrl?> GetByIdAsync(string id, CancellationToken ct)
        => db.ShortUrls.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<ShortUrl?> GetByIdAsNoTrackingAsync(string id, CancellationToken ct)
        => db.ShortUrls.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task AddAsync(ShortUrl entity, CancellationToken ct)
        => db.ShortUrls.AddAsync(entity, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<ShortUrl>> ListAsync(int skip, int take, CancellationToken ct)
    {
        return await db.ShortUrls
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task DeleteAsync(ShortUrl entity, CancellationToken ct)
    {
        db.ShortUrls.Remove(entity);
        return Task.CompletedTask;
    }
}