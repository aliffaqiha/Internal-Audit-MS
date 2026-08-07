using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.CorrectiveActions;

public sealed record StartCapCommand(Guid Id) : IRequest;

public sealed class StartCapCommandValidator : AbstractValidator<StartCapCommand>
{
    public StartCapCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

internal sealed class StartCapCommandHandler : IRequestHandler<StartCapCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public StartCapCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task Handle(StartCapCommand request, CancellationToken cancellationToken)
    {
        var cap = await _db.CorrectiveActions
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Rencana tindak lanjut tidak ditemukan.");

        CapState.EnsureTransition(cap, CorrectiveActionStatus.Open, CorrectiveActionStatus.InProgress, "Mulai CAP");

        cap.Status = CorrectiveActionStatus.InProgress;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Cap.Started", nameof(CorrectiveAction), cap.Id.ToString(),
            oldValues: "Open", newValues: "InProgress", cancellationToken: cancellationToken);
    }
}