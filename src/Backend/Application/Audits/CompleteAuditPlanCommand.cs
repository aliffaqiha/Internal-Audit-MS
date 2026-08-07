using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Audits;

public sealed record CompleteAuditPlanCommand(Guid AuditPlanId) : IRequest;

public sealed class CompleteAuditPlanCommandValidator : AbstractValidator<CompleteAuditPlanCommand>
{
    public CompleteAuditPlanCommandValidator() => RuleFor(x => x.AuditPlanId).NotEmpty();
}

internal sealed class CompleteAuditPlanCommandHandler : IRequestHandler<CompleteAuditPlanCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public CompleteAuditPlanCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task Handle(CompleteAuditPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.AuditPlans
            .FirstOrDefaultAsync(p => p.Id == request.AuditPlanId, cancellationToken)
            ?? throw new KeyNotFoundException("Rencana audit tidak ditemukan.");

        AuditState.EnsureTransition(plan, AuditPlanStatus.InProgress, AuditPlanStatus.Completed, "diselesaikan");

        var pending = await _db.AuditChecklistItems.CountAsync(
            i => i.AuditPlanId == plan.Id && i.Status == ChecklistItemStatus.Pending,
            cancellationToken);

        if (pending > 0)
            throw new InvalidOperationException(
                $"Selesaikan dulu {pending} item checklist yang masih berstatus Pending.");

        plan.Status = AuditPlanStatus.Completed;
        plan.EndDate ??= DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("AuditPlan.Completed", nameof(AuditPlan), plan.Id.ToString(),
            oldValues: "InProgress", newValues: "Completed", cancellationToken: cancellationToken);
    }
}