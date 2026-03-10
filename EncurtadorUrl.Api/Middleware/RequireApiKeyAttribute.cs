namespace UrlShortener.Api.Middleware;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireApiKeyAttribute : Attribute { }