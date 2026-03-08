using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Domain;
using UrlShortener.Api.Repositories;
using UrlShortener.Api.Resources;

namespace UrlShortener.Api.Services;

public sealed class ShortUrlService(
    IShortUrlRepository repo,
    IConfiguration config,
    ILogger<ShortUrlService> logger)
{
    private readonly string _baseUrl = config["Shortener:BaseUrl"] ?? "http://localhost:8080";
    private readonly int _generatedCodeLength = int.TryParse(config["Shortener:GeneratedCodeLength"], out var v) ? Math.Clamp(v, 4, 12) : 5;
    private const int MaxGenerateAttempts = 6;

    public async Task<ShortUrlResponse> CreateAsync(CreateShortUrlRequest request, CancellationToken ct)
    {
        UrlValidator.EnsureValidHttpUrl(request.OriginalUrl);

        if (request.ExpirationDate is not null && request.ExpirationDate <= DateTimeOffset.UtcNow)
            throw new ValidationException("expirationDate deve estar no futuro.");

        var originalUrl = request.OriginalUrl!.Trim();

        if (!string.IsNullOrWhiteSpace(request.CustomAlias))
        {
            var alias = request.CustomAlias.Trim();
            UrlValidator.EnsureValidAlias(alias);

            if (await repo.IdExistsAsync(alias, ct))
                throw new ConflictException("customAlias já está em uso.");

            var entityWithAlias = new ShortUrl(id: alias, code: alias, originalUrl: originalUrl, expirationDate: request.ExpirationDate);

            await repo.AddAsync(entityWithAlias, ct);

            try
            {
                await repo.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                throw new ConflictException("customAlias já está em uso.");
            }

            logger.LogInformation("Short URL criada com alias: {Code} -> {OriginalUrl}", entityWithAlias.Code, entityWithAlias.OriginalUrl);
            return ToResponse(entityWithAlias);
        }

        for (int attempt = 0; attempt < MaxGenerateAttempts; attempt++)
        {
            var id = Base62.GenerateRandom(_generatedCodeLength);

            if (await repo.IdExistsAsync(id, ct))
            {
                logger.LogDebug("Generated code already exists, retrying: {Id}", id);
                continue;
            }

            var entity = new ShortUrl(id: id, code: id, originalUrl: originalUrl, expirationDate: request.ExpirationDate);
            await repo.AddAsync(entity, ct);

            try
            {
                await repo.SaveChangesAsync(ct);
                logger.LogInformation("Short URL criada: {Code} -> {OriginalUrl}", entity.Code, entity.OriginalUrl);
                return ToResponse(entity);
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "DbUpdateException ao salvar código gerado. Tentativa {Attempt}", attempt + 1);
                try
                {
                    var ctx = repo as Microsoft.EntityFrameworkCore.DbContext;
                    if (ctx is not null)
                    {
                        var entry = ctx.Entry(entity);
                        if (entry is not null) entry.State = EntityState.Detached;
                    }
                }
                catch
                {
                }
            }
        }

        throw new ConflictException("Não foi possível gerar um código único para a URL após várias tentativas.");
    }

    public async Task<ShortUrlResponse> GetDetailsAsync(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ValidationException("id é obrigatório.");

        var entity = await repo.GetByIdAsNoTrackingAsync(id, ct);
        if (entity is null)
            throw new NotFoundException("ID não encontrado.");

        if (entity.IsExpired(DateTimeOffset.UtcNow))
            throw new ExpiredException("URL expirada.");

        return ToResponse(entity);
    }

    public async Task<string> ResolveAndCountClickAsync(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ValidationException("ID é obrigatório.");

        var entity = await repo.GetByIdAsync(id, ct);
        if (entity is null)
            throw new NotFoundException("ID não encontrado.");

        if (entity.IsExpired(DateTimeOffset.UtcNow))
            throw new ExpiredException("URL expirada.");

        entity.RegisterClick();
        await repo.SaveChangesAsync(ct);

        return entity.OriginalUrl;
    }

    private ShortUrlResponse ToResponse(ShortUrl entity) => new()
    {
        Id = entity.Id.ToString(),
        CustomAlias = entity.Code,
        ShortUrl = $"{_baseUrl.TrimEnd('/')}/{entity.Code}",
        OriginalUrl = entity.OriginalUrl,
        CreatedAt = entity.CreatedAt,
        ExpirationDate = entity.ExpirationDate,
        ClickCount = entity.ClickCount
    };

    public async Task<IReadOnlyList<ShortUrlResponse>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) throw new ValidationException("page deve ser >= 1.");
        if (pageSize < 1 || pageSize > 100) throw new ValidationException("pageSize deve estar entre 1 e 100.");

        var skip = (page - 1) * pageSize;

        var items = await repo.ListAsync(skip, pageSize, ct);
        return items.Select(ToResponse).ToList();
    }

    public async Task DeleteAsync(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ValidationException("id é obrigatório.");

        var entity = await repo.GetByIdAsync(id, ct);
        if (entity is null)
            throw new NotFoundException("ID não encontrado.");

        await repo.DeleteAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }
}