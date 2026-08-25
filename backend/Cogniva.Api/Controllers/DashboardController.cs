using Cogniva.Api.DTOs.Dashboard;
using Cogniva.Api.Middleware;
using Cogniva.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cogniva.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(
    IDashboardService dashboardService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await dashboardService.GetAsync(GetUserId(), cancellationToken));

    private Guid GetUserId() => currentUserService.UserId
        ?? throw new ApiException(401, "Prijava je neophodna.", "Prijavite se da biste otvorili kontrolnu tablu.");
}
