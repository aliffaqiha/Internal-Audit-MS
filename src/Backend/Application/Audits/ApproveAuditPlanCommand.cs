using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using IAMS.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Audits;

public sealed record ApproveAuditPlanCommand(Guid AuditPlanId, string? Comment) : IRequest;

public sealed class ApproveAuditPlanCommandValidator : AbstractValidator<ApproveAuditPlanCommand>
{
    public ApproveAuditPlanCommandValidator()
    {
        RuleFor(x => x.AuditPlanId).NotEmpty();
        RuleFor(x => x.Comment).MaximumLength(500);
    }
}

internal sealed class ApproveAuditPlanCommandHandler : IRequestHandler<ApproveAuditPlanCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly IPublisher _publisher;

    public ApproveAuditPlanCommandHandler(IApplicationDbContext db, IAuditService audit, IPublisher publisher)
    {
        _db = db;
        _audit = audit;
        _publisher = publisher;
    }

    public async Task Handle(ApproveAuditPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.AuditPlans
            .FirstOrDefaultAsync(p => p.Id == request.AuditPlanId, cancellationToken)
            ?? throw new KeyNotFoundException("Rencana audit tidak ditemukan.");

        AuditState.EnsureTransition(plan, AuditPlanStatus.Submitted, AuditPlanStatus.Approved, "disetujui");

        plan.Status = AuditPlanStatus.Approved;
        plan.RejectionReason = request.Comment;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("AuditPlan.Approved", nameof(AuditPlan), plan.Id.ToString(),
            oldValues: "Submitted", newValues: "Approved", cancellationToken: cancellationToken);

        await _publisher.Publish(new AuditPlanApprovedEvent(plan.Id, plan.Title), cancellationToken);
    }
}