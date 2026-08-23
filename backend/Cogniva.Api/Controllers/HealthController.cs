using Microsoft.AspNetCore.Mvc;

namespace Cogniva.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get()
    {
        return Ok(new HealthResponse("Healthy", DateTimeOffset.UtcNow));
    }
}

public sealed record HealthResponse(string Status, DateTimeOffset Timestamp);
