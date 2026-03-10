using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UrlShortener.Api.Middleware;

public sealed class RequireApiKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requiresApiKey = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<RequireApiKeyAttribute>()
            .Any();

        if (!requiresApiKey) return;

        operation.Security ??= new List<OpenApiSecurityRequirement>();

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    }
                }
            ] = new List<string>()
        });
    }
}