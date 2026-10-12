using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Relovia.Api.Features.Places;

namespace Relovia.Api.Controllers;

[ApiController]
[Route("api/places")]
public sealed class CitiesController(CityCatalog cities) : ControllerBase
{
    [HttpGet("cities")]
    [EndpointName("listCities")]
    [EndpointSummary("Lists city names for a country.")]
    public async Task<ActionResult<CitySearchResponse>> List(
        [FromQuery] string? country,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out _))
        {
            return Unauthorized();
        }

        var code = country?.Trim() ?? string.Empty;
        if (code.Length != 2 || !code.All(char.IsAsciiLetter))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Enter a two-letter country code.");
        }

        try
        {
            var names = await cities.ListAsync(code, cancellationToken);
            return Ok(new CitySearchResponse(names));
        }
        catch (CityCatalogException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: exception.Message);
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId) && userId != Guid.Empty;
    }
}
