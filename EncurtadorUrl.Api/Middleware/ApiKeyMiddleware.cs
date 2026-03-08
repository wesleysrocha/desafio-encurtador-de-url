using UrlShortener.Api.Services;

namespace UrlShortener.Api.Middleware;

public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
{
    private readonly string? _apiKey = config["Shortener:ApiKey"];

    public async Task InvokeAsync(HttpContext context)
    {
        // Protege apenas a criação: POST /v1/urls
        if (context.Request.Path.Equals("/v1/urls", StringComparison.OrdinalIgnoreCase) &&
            HttpMethods.IsPost(context.Request.Method))
        {
            var provided = context.Request.Headers["X-API-Key"].FirstOrDefault();

            // Se a API Key não estiver configurada no appsettings, falha por segurança
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new UnauthorizedException("API Key não configurada no servidor.");

            if (string.IsNullOrWhiteSpace(provided) || provided != _apiKey)
                throw new UnauthorizedException("API Key inválida ou ausente (header X-API-Key).");
        }

        await next(context);
    }
}