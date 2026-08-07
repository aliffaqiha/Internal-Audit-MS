using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Audits;

public sealed record StartAuditPlanCommand(Guid AuditPlanId) : IRequest;

public sealed class StartAuditPlanCommandValidator : AbstractValidator<StartAuditPlanCommand>
{
    public StartAuditPlanCommandValidator() => RuleFor(x => x.AuditPlanId).NotEmpty();
}

internal sealed class StartAuditPlanCommandHandler : IRequestHandler<StartAuditPlanCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public StartAuditPlanCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task Handle(StartAuditPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.AuditPlans
            .Include(p => p.ChecklistItems)
            .FirstOrDefaultAsync(p => p.Id == request.AuditPlanId, cancellationToken)
            ?? throw new KeyNotFoundException("Rencana audit tidak ditemukan.");

        AuditState.EnsureTransition(plan, AuditPlanStatus.Approved, AuditPlanStatus.InProgress, "dimulai");

        if (plan.ChecklistItems.Count == 0)
            throw new InvalidOperationException("Rencana audit perlu checklist sebelum dimulai.");

        plan.Status = AuditPlanStatus.InProgress;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("AuditPlan.Started", nameof(AuditPlan), plan.Id.ToString(),
            oldValues: "Approved", newValues: "InProgress", cancellationToken: cancellationToken);
    }
}