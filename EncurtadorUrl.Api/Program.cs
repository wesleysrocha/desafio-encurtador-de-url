using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using UrlShortener.Api.Data;
using UrlShortener.Api.Middleware;
using UrlShortener.Api.Repositories;
using UrlShortener.Api.Services;

try
{
    // Carrega variáveis de um arquivo .env (na raiz do repo ou em uma pasta acima).
    // Opcional: em Docker/produção as variáveis já vêm do ambiente do container.
    Env.TraversePath().Load();
}
catch (Exception)
{
    // Sem .env disponível: seguimos com as variáveis de ambiente/appsettings existentes.
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var mySqlConnectionString = BuildMySqlConnectionString(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(mySqlConnectionString, new MySqlServerVersion(new Version(8, 0, 36))));

builder.Services.AddScoped<IShortUrlRepository, ShortUrlRepository>();
builder.Services.AddScoped<IShortUrlService, ShortUrlService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "URL Shortener API",
        Version = "v1",
        Description = "API para encurtamento de URLs (processo seletivo)."
    });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Header: X-API-Key: {token}. Obrigatório apenas no POST /v1/urls",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-API-Key",
        In = ParameterLocation.Header
    });

    c.OperationFilter<RequireApiKeyOperationFilter>();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.DocumentTitle = "URL Shortener API";
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
});

app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

app.Run();

// Monta a connection string do MySQL a partir de variáveis de ambiente
// (DB_HOST, DB_PORT, DB_USER, DB_PASSWORD, DB_NAME), com fallback para
// valores em appsettings.json (seção "Database") e por fim valores padrão.
static string BuildMySqlConnectionString(IConfiguration configuration)
{
    var host = Environment.GetEnvironmentVariable("DB_HOST") ?? configuration["Database:Host"] ?? "localhost";
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? configuration["Database:Port"] ?? "3306";
    var user = Environment.GetEnvironmentVariable("DB_USER") ?? configuration["Database:User"] ?? "root";
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? configuration["Database:Password"] ?? string.Empty;
    var name = Environment.GetEnvironmentVariable("DB_NAME") ?? configuration["Database:Name"] ?? "url_shortener";

    return $"Server={host};Port={port};Database={name};User={user};Password={password};";
}

public partial class Program { }
