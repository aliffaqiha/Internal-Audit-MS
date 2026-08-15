using FluentValidation;
using IAMS.Application.Common.DataScoping;
using IAMS.Application.Common.Exceptions;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AppValidationException = IAMS.Application.Common.Exceptions.ValidationException;

namespace IAMS.Application.CorrectiveActions;

public sealed record CreateCapCommand(
    Guid FindingId,
    string Action,
    string? PicName,
    DateTimeOffset? TargetDate,
    int Progress = 0) : IRequest<Guid>;

public sealed class CreateCapCommandValidator : AbstractValidator<CreateCapCommand>
{
    public CreateCapCommandValidator()
    {
        RuleFor(x => x.Action).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.PicName).MaximumLength(150);
        RuleFor(x => x.Progress).InclusiveBetween(0, 100);
    }
}

internal sealed class CreateCapCommandHandler : IRequestHandler<CreateCapCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;

    public CreateCapCommandHandler(IApplicationDbContext db, IAuditService audit, ICurrentUserService currentUser)
    {
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCapCommand request, CancellationToken cancellationToken)
    {
        var scope = await CurrentUserAccess.ResolveAsync(_db, _currentUser, cancellationToken);

        var finding = await _db.Findings
            .FirstOrDefaultAsync(f => f.Id == request.FindingId, cancellationToken)
            ?? throw new KeyNotFoundException("Temuan tidak ditemukan.");

        CurrentUserAccess.EnsureCanAccessFinding(scope, finding.DepartmentId);

        var existing = await _db.CorrectiveActions
            .AnyAsync(c => c.FindingId == request.FindingId, cancellationToken);
        if (existing)
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                ["FindingId"] = new[] { "Temuan ini sudah memiliki rencana tindak lanjut (CAP)." }
            });

        var cap = new CorrectiveAction
        {
            FindingId = finding.Id,
            Action = request.Action.Trim(),
            PicName = request.PicName?.Trim(),
            TargetDate = request.TargetDate,
            Progress = request.Progress,
            Status = CorrectiveActionStatus.Open
        };

        _db.CorrectiveActions.Add(cap);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Cap.Created", nameof(CorrectiveAction), cap.Id.ToString(),
            oldValues: finding.Id.ToString(), newValues: cap.Action,
            cancellationToken: cancellationToken);

        return cap.Id;
    }
}