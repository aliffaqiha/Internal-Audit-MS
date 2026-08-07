using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Audits;

public sealed record SubmitAuditPlanCommand(Guid AuditPlanId) : IRequest;

public static class AuditState
{
    public static void EnsureTransition(AuditPlan plan, AuditPlanStatus expected, AuditPlanStatus next, string action)
    {
        if (plan.Status != expected)
            throw new InvalidOperationException(
                $"Rencana audit tidak dapat {action} dari status '{plan.Status}'.");
    }
}

public sealed class SubmitAuditPlanCommandValidator : AbstractValidator<SubmitAuditPlanCommand>
{
    public SubmitAuditPlanCommandValidator() => RuleFor(x => x.AuditPlanId).NotEmpty();
}

internal sealed class SubmitAuditPlanCommandHandler : IRequestHandler<SubmitAuditPlanCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public SubmitAuditPlanCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task Handle(SubmitAuditPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.AuditPlans
            .Include(p => p.ChecklistItems)
            .FirstOrDefaultAsync(p => p.Id == request.AuditPlanId, cancellationToken)
            ?? throw new KeyNotFoundException("Rencana audit tidak ditemukan.");

        AuditState.EnsureTransition(plan, AuditPlanStatus.Draft, AuditPlanStatus.Submitted, "disubmit");

        if (plan.ChecklistItems.Count == 0)
            throw new InvalidOperationException("Rencana audit perlu checklist minimal satu item sebelum disubmit.");

        plan.Status = AuditPlanStatus.Submitted;
        plan.RejectionReason = null;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("AuditPlan.Submitted", nameof(AuditPlan), plan.Id.ToString(),
            oldValues: "Draft", newValues: "Submitted", cancellationToken: cancellationToken);
    }
}