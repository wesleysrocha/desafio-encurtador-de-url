using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Middleware;
using UrlShortener.Api.Resources;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("v1/urls")]
public sealed class UrlsController(IShortUrlService service) : ControllerBase
{
    [HttpPost]
    [RequireApiKey]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShortUrlResponse>> Create(
        [FromBody] CreateShortUrlRequest request,
        CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);

        return CreatedAtAction(
            actionName: nameof(GetDetails),
            routeValues: new { id = created.Id },
            value: created);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<ActionResult<ShortUrlResponse>> GetDetails(
        [FromRoute] string id,
        CancellationToken ct)
    {
        var result = await service.GetDetailsAsync(id, ct);
        return Ok(result);
    }

    //url de paginacao
    [HttpGet]
    [ProducesResponseType(typeof(List<ShortUrlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ShortUrlResponse>>> List(
       [FromQuery] int page = 1,
       [FromQuery] int pageSize = 50,
       CancellationToken ct = default)
    {
        var result = await service.ListAsync(page, pageSize, ct);
        return Ok(result.ToList());
    }

    //url de delete
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete([FromRoute] string id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}