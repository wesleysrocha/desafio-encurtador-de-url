using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
public sealed class RedirectController(ShortUrlService service) : ControllerBase
{
    [HttpGet("{id:regex(^[[0-9A-Za-z]]{{5,64}}$)}")]
    public async Task<IActionResult> RedirectToOriginal([FromRoute] string id, CancellationToken ct)
    {
        var originalUrl = await service.ResolveAndCountClickAsync(id, ct);
        return Ok(originalUrl);
    }
}