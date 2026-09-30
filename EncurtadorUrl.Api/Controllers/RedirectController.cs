using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
public sealed class RedirectController(IShortUrlService service) : ControllerBase
{
    // Redireciona (302 Found) para a URL original e contabiliza o clique.
    // Assim o link curto funciona direto no navegador, como no bitly.
    [HttpGet("{id:regex(^[[0-9A-Za-z]]{{5,64}}$)}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> RedirectToOriginal([FromRoute] string id, CancellationToken ct)
    {
        var originalUrl = await service.ResolveAndCountClickAsync(id, ct);
        return Redirect(originalUrl);
    }
}