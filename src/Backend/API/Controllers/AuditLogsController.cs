using IAMS.Application.AuditLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IAMS.Api.Controllers;

/// <summary>Read-only audit trail for administrators (who changed what, when, from where).</summary>
[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = IAMS.Api.Authorization.Policies.Administrator)]
public sealed class AuditLogsController : ControllerBase
{
    private readonly ISender _sender;

    public AuditLogsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> Get(
        [FromQuery] GetAuditLogsQuery query, CancellationToken ct = default)
        => Ok(await _sender.Send(query, ct));
}