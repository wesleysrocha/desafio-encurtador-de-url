using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            var (status, title) = ex switch
            {
                ValidationException => (StatusCodes.Status400BadRequest, "Validation error"),
                UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
                ExpiredException => (StatusCodes.Status410Gone, "Gone"),
                _ => (StatusCodes.Status500InternalServerError, "Internal server error")
            };

            if (status >= 500)
                logger.LogError(ex, "Erro inesperado (traceId: {TraceId})", traceId);
            else
                logger.LogWarning(ex, "Erro tratado: {Message} (traceId: {TraceId})", ex.Message, traceId);

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = ex.Message,
                Instance = context.Request.Path
            };

            problem.Extensions["traceId"] = traceId;

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}