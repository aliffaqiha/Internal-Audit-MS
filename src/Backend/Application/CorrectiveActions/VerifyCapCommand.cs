using FluentValidation;
using IAMS.Application.Common.DataScoping;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.CorrectiveActions;

public sealed record VerifyCapCommand(bool Approve, string? Note, Guid Id) : IRequest;

public sealed class VerifyCapCommandValidator : AbstractValidator<VerifyCapCommand>
{
    public VerifyCapCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

internal sealed class VerifyCapCommandHandler : IRequestHandler<VerifyCapCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;

    public VerifyCapCommandHandler(
        IApplicationDbContext db, IAuditService audit, ICurrentUserService currentUser)
    {
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task Handle(VerifyCapCommand request, CancellationToken cancellationToken)
    {
        var scope = await CurrentUserAccess.ResolveAsync(_db, _currentUser, cancellationToken);

        var cap = await _db.CorrectiveActions
            .Include(c => c.Finding)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Rencana tindak lanjut tidak ditemukan.");

        CurrentUserAccess.EnsureCanAccessFinding(scope, cap.Finding?.DepartmentId);

        CapState.EnsureTransition(cap, CorrectiveActionStatus.PendingVerification,
            CorrectiveActionStatus.Closed, "Verifikasi CAP");

        if (request.Approve)
        {
            cap.Status = CorrectiveActionStatus.Closed;
            cap.VerificationNote = request.Note?.Trim();
            cap.VerifiedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            // Reject => reopen back to In Progress so the auditee can amend and resubmit.
            cap.Status = CorrectiveActionStatus.InProgress;
            cap.RejectionReason = request.Note?.Trim();
            cap.VerificationNote = null;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            request.Approve ? "Cap.Approved" : "Cap.Rejected",
            nameof(CorrectiveAction), cap.Id.ToString(),
            oldValues: "PendingVerification",
            newValues: request.Approve ? "Closed" : "InProgress(reopened)",
            cancellationToken: cancellationToken);
    }
}