using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Domain;

namespace UrlShortener.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ShortUrl> ShortUrls => Set<ShortUrl>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ShortUrl>(entity =>
        {
            entity.ToTable("short_urls");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .HasMaxLength(64)
                .IsRequired()
                .HasColumnType("string");

            entity.Property(x => x.Code)
                .HasColumnName("code")
                .HasMaxLength(12)
                .IsRequired();

            entity.HasIndex(x => x.Id)
                .IsUnique();

            entity.Property(x => x.OriginalUrl)
                .HasColumnName("original_url")
                .HasMaxLength(2048)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasConversion(
                    v => v.UtcDateTime,
                    v => new DateTimeOffset(DateTime.SpecifyKind(v, DateTimeKind.Utc)));

            entity.Property(x => x.ExpirationDate)
                .HasColumnName("expiration_date")
                .HasConversion(
                    v => v.HasValue ? v.Value.UtcDateTime : (DateTime?)null,
                    v => v.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
                        : (DateTimeOffset?)null);

            entity.Property(x => x.ClickCount)
                .HasColumnName("click_count")
                .HasDefaultValue(0);
        });
    }
}