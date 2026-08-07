using IAMS.Api.Authorization;
using IAMS.Application.Audits;
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

    public AuditController(ISender sender)
    {
        _sender = sender;
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
}