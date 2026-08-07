using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.CorrectiveActions;

public sealed record SubmitCapCommand(Guid Id) : IRequest;

public sealed class SubmitCapCommandValidator : AbstractValidator<SubmitCapCommand>
{
    public SubmitCapCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

internal sealed class SubmitCapCommandHandler : IRequestHandler<SubmitCapCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public SubmitCapCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task Handle(SubmitCapCommand request, CancellationToken cancellationToken)
    {
        var cap = await _db.CorrectiveActions
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Rencana tindak lanjut tidak ditemukan.");

        CapState.EnsureTransition(cap, CorrectiveActionStatus.InProgress,
            CorrectiveActionStatus.PendingVerification, "Ajukan verifikasi");

        if (cap.Progress < 100)
            throw new InvalidOperationException("CAP hanya dapat diajukan verifikasi saat progress mencapai 100%.");

        cap.Status = CorrectiveActionStatus.PendingVerification;
        cap.VerificationNote = null;
        cap.RejectionReason = null;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Cap.Submitted", nameof(CorrectiveAction), cap.Id.ToString(),
            oldValues: "InProgress", newValues: "PendingVerification",
            cancellationToken: cancellationToken);
    }
}