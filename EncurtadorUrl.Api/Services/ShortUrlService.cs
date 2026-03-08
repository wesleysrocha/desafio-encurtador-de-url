using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
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

    public async Task<ShortUrlResponse> CreateAsync(CreateShortUrlRequest request, CancellationToken ct)
    {
        UrlValidator.EnsureValidHttpUrl(request.OriginalUrl);

        if (request.ExpirationDate is not null && request.ExpirationDate <= DateTimeOffset.UtcNow)
            throw new ValidationException("expirationDate deve estar no futuro.");

        var originalUrl = request.OriginalUrl!.Trim();

        // Se o usuário mandou alias, ele vira o "Code"
        if (!string.IsNullOrWhiteSpace(request.CustomAlias))
        {
            var alias = request.CustomAlias.Trim();
            UrlValidator.EnsureValidAlias(alias);

            if (await repo.CodeExistsAsync(alias, ct))
                throw new ConflictException("customAlias já está em uso.");

            var entityWithAlias = new ShortUrl(code: alias, originalUrl: originalUrl, expirationDate: request.ExpirationDate);

            await repo.AddAsync(entityWithAlias, ct);

            try
            {
                 await repo.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Caso raro: corrida entre CodeExistsAsync e SaveChanges
                throw new ConflictException("customAlias já está em uso.");
            }

            logger.LogInformation("Short URL criada com alias: {Code} -> {OriginalUrl}", entityWithAlias.Code, entityWithAlias.OriginalUrl);
            return ToResponse(entityWithAlias);
        }

        // Sem alias: cria com um code temporário único para pegar o Id autoincrement
        var tempCode = $"tmp_{Guid.NewGuid():N}";

        var entity = new ShortUrl(code: tempCode, originalUrl: originalUrl, expirationDate: request.ExpirationDate);

        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct); // aqui o Id é gerado

        // Code final deterministicamente baseado no Id (sem colisão)
        var finalCode = Base62.Encode(entity.Id);
        entity.SetCode(finalCode);

        try
        {
            await repo.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Extremamente improvável (Id->Base62 é determinístico), mas deixa consistente
            throw new ConflictException("Não foi possível gerar um código único para a URL.");
        }

        logger.LogInformation("Short URL criada: {Code} -> {OriginalUrl}", entity.Code, entity.OriginalUrl);
        return ToResponse(entity);
    }

    public async Task<ShortUrlResponse> GetDetailsAsync(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ValidationException("id é obrigatório.");

        var entity = await repo.GetByCodeAsNoTrackingAsync(code, ct);
        if (entity is null)
            throw new NotFoundException("ID não encontrado.");

        if (entity.IsExpired(DateTimeOffset.UtcNow))
            throw new ExpiredException("URL expirada.");

        return ToResponse(entity);
    }

    public async Task<string> ResolveAndCountClickAsync(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ValidationException("id é obrigatório.");

        var entity = await repo.GetByCodeAsync(code, ct);
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
        Id = entity.Code,
        ShortUrl = $"{_baseUrl.TrimEnd('/')}/{entity.Code}",
        OriginalUrl = entity.OriginalUrl,
        CreatedAt = entity.CreatedAt,
        ExpirationDate = entity.ExpirationDate,
        ClickCount = entity.ClickCount
    };

    //url paginacao
    public async Task<IReadOnlyList<ShortUrlResponse>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) throw new ValidationException("page deve ser >= 1.");
        if (pageSize < 1 || pageSize > 100) throw new ValidationException("pageSize deve estar entre 1 e 100.");

        var skip = (page - 1) * pageSize;

        var items = await repo.ListAsync(skip, pageSize, ct);
        return items.Select(ToResponse).ToList();
    }

    //url delete
    public async Task DeleteAsync(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ValidationException("id é obrigatório.");

        var entity = await repo.GetByCodeAsync(code, ct);
        if (entity is null)
            throw new NotFoundException("ID não encontrado.");

        await repo.DeleteAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }
}