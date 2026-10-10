using Microsoft.AspNetCore.Mvc;

namespace Relovia.Api.Controllers;

[ApiController]
public sealed class ServiceInfoController : ControllerBase
{
    [HttpGet("/")]
    [EndpointSummary("Returns the service name.")]
    public ActionResult<ServiceInfoResponse> Get()
    {
        return Ok(new ServiceInfoResponse("Relovia"));
    }
}

public sealed record ServiceInfoResponse(string Name);
