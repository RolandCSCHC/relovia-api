using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Relovia.Api.Controllers;

[ApiController]
[AllowAnonymous]
public sealed class ServiceInfoController : ControllerBase
{
    [HttpGet("/")]
    [EndpointName("getServiceInfo")]
    [EndpointSummary("Returns the service name.")]
    public ActionResult<ServiceInfoResponse> Get()
    {
        return Ok(new ServiceInfoResponse("Relovia"));
    }
}

public sealed record ServiceInfoResponse(string Name);
