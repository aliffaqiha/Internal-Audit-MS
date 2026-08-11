using IAMS.Api.Authorization;
using IAMS.Application.AuditReports;
using IAMS.Application.Audits;
using IAMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IAMS.Api.Controllers;

[ApiController]
[Route("api/audits")]
[Authorize]
public sealed class AuditController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IObjectStorageService _storage;

    public AuditController(ISender sender, IObjectStorageService storage)
    {
        _sender = sender;
        _storage = storage;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuditPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditPlanDto>>> Get(
        [FromQuery] GetAuditPlansQuery query, CancellationToken ct)
        => Ok(await _sender.Send(query, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AuditPlanDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditPlanDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _sender.Send(new GetAuditPlanByIdQuery(id), ct));

    [HttpGet("team")]
    [Authorize(Policy = Policies.AuditPlanner)]
    [ProducesResponseType(typeof(IReadOnlyList<AuditTeamMemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditTeamMemberDto>>> Team(CancellationToken ct)
        => Ok(await _sender.Send(new GetAuditTeamQuery(), ct));

    [HttpPost]
    [Authorize(Policy = Policies.AuditPlanner)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateAuditPlanCommand command, CancellationToken ct)
    {
        var id = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = Policies.AuditPlanner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        await _sender.Send(new SubmitAuditPlanCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Policies.AuditApprover)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveAuditPlanCommand? command, CancellationToken ct)
    {
        await _sender.Send((command ?? new ApproveAuditPlanCommand(id, null)) with { AuditPlanId = id }, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = Policies.AuditPlanner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        await _sender.Send(new StartAuditPlanCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Policies.AuditPlanner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new CompleteAuditPlanCommand(id), ct);
        return NoContent();
    }

    [HttpPut("{auditPlanId:guid}/checklist/{itemId:guid}")]
    [Authorize(Policy = Policies.AuditPlanner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateChecklistItem(
        Guid auditPlanId, Guid itemId, UpdateChecklistItemCommand command, CancellationToken ct)
    {
        await _sender.Send(command with { AuditPlanId = auditPlanId, ItemId = itemId }, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/report/meta")]
    [ProducesResponseType(typeof(AuditReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditReportDto>> GetReportMeta(Guid id, CancellationToken ct)
    {
        var report = await _sender.Send(new GetAuditReportQuery(id), ct);
        return report is null ? NotFound() : Ok(report);
    }

    [HttpPost("{id:guid}/report")]
    [Authorize(Policy = Policies.AuditPlanner)]
    [ProducesResponseType(typeof(AuditReportDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AuditReportDto>> GenerateReport(Guid id, CancellationToken ct)
    {
        var report = await _sender.Send(new GenerateAuditReportCommand(id), ct);
        return CreatedAtAction(nameof(GetReportMeta), new { id }, report);
    }

    [HttpGet("{id:guid}/report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadReport(Guid id, CancellationToken ct)
    {
        var report = await _sender.Send(new GetAuditReportQuery(id), ct);
        if (report is null)
            return NotFound();

        var stream = await _storage.GetAsync(report.ObjectName, ct);
        if (stream is null)
            return NotFound();

        return File(stream, report.ContentType, report.FileName);
    }
}