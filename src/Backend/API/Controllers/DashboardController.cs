using IAMS.Api.Authorization;
using IAMS.Application.Analytics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IAMS.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly ISender _sender;

    public DashboardController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("analytics")]
    [Authorize(Policy = Policies.DashboardView)]
    [ProducesResponseType(typeof(DashboardAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DashboardAnalyticsDto>> Analytics(CancellationToken ct)
        => Ok(await _sender.Send(new GetDashboardAnalyticsQuery(), ct));
}
