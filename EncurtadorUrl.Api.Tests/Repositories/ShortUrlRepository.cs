using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Domain;
using UrlShortener.Api.Repositories;

namespace UrlShortener.Tests.Repositories;

public sealed class ShortUrlRepositoryTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ShortUrlRepository _repo;

    public ShortUrlRepositoryTests()
    {
        // SQLite in-memory precisa manter a conexão aberta durante o teste
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _repo = new ShortUrlRepository(_db);
    }

    public async Task InitializeAsync()
    {
        await _db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact(DisplayName = "inserido ID no banco com sucesso")]
    public async Task Add_And_GetById_ShouldReturnEntity()
    {
        var entity = new ShortUrl(
            id: "abc12",
            code: "aliasX",
            originalUrl: "https://example.com",
            expirationDate: DateTimeOffset.UtcNow.AddMinutes(5));


        await _repo.AddAsync(entity, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var found = await _repo.GetByIdAsync("abc12", CancellationToken.None);

        found.Should().NotBeNull();
        found!.Id.Should().Be("abc12");
        found.Code.Should().Be("aliasX");
        found.OriginalUrl.Should().Be("https://example.com");
    }


    [Fact(DisplayName = "ID existe deve retornar o conteúdo")]
    public async Task IdExistsAsync_WhenExists_ShouldReturnTrue()
    {
        var entity = new ShortUrl(
              id: "id01",
              code: "aliasX",
              originalUrl: "https://example.com",
              expirationDate: DateTimeOffset.UtcNow.AddMinutes(5));

        await _repo.AddAsync(entity, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var exists = await _repo.IdExistsAsync("id01", CancellationToken.None);
        exists.Should().BeTrue();
    }

    [Fact(DisplayName = "Custom Alias existe deve retornar o conteúdo")]
    public async Task CodeExistsAsync_WhenExists_ShouldReturnTrue()
    {
        var entity = new ShortUrl(
              id: "id02",
              code: "code02",
              originalUrl: "https://b.com",
              expirationDate: DateTimeOffset.UtcNow.AddMinutes(5));

        await _repo.AddAsync(entity, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var exists = await _repo.CodeExistsAsync("code02", CancellationToken.None);
        exists.Should().BeTrue();
    }

    [Fact(DisplayName = "listar URLs paginada com dois registros mais recentes")]
    public async Task ListAsync_ShouldReturnOrderedByCreatedAtDesc_WithPagination()
    {
        var now = DateTimeOffset.UtcNow;

        var e1 = new ShortUrl("id1", "c1", "https://1.com", now.AddMinutes(-4));
        var e2 = new ShortUrl("id2", "c2", "https://2.com", now.AddMinutes(-5));
        var e3 = new ShortUrl("id3", "c3", "https://3.com", now.AddMinutes(-1));

        await _repo.AddAsync(e1, CancellationToken.None);
        await _repo.AddAsync(e2, CancellationToken.None);
        await _repo.AddAsync(e3, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var result = await _repo.ListAsync(skip: 0, take: 2, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("id3");
        result[1].Id.Should().Be("id2");
    }

    [Fact(DisplayName = "deletando URL por ID")]
    public async Task DeleteAsync_ShouldRemoveEntity()
    {
        var entity = new ShortUrl(
              id: "idDel",
              code: "aliasX",
              originalUrl: "https://example.com",
              expirationDate: DateTimeOffset.UtcNow.AddMinutes(5));

        await _repo.AddAsync(entity, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var found = await _repo.GetByIdAsync("idDel", CancellationToken.None);
        found.Should().NotBeNull();

        await _repo.DeleteAsync(found!, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var after = await _repo.GetByIdAsync("idDel", CancellationToken.None);
        after.Should().BeNull();
    }

    [Fact(DisplayName = "ID não encontrado")]
    public async Task GetByIdAsNoTracking_ShouldReturnEntity()
    {
        var entity = new ShortUrl(
              id: "id03",
              code: "aliasX",
              originalUrl: "https://example.com",
              expirationDate: DateTimeOffset.UtcNow.AddMinutes(5));

        await _repo.AddAsync(entity, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var found = await _repo.GetByIdAsNoTrackingAsync("id03", CancellationToken.None);

        found.Should().NotBeNull();
        found!.Id.Should().Be("id03");
    }
}