using FluentValidation;
using IAMS.Application.Common.DataScoping;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.CorrectiveActions;

public sealed record UpdateCapCommand(
    string Action,
    string? PicName,
    DateTimeOffset? TargetDate,
    int Progress,
    Guid Id) : IRequest;

public sealed class UpdateCapCommandValidator : AbstractValidator<UpdateCapCommand>
{
    public UpdateCapCommandValidator()
    {
        RuleFor(x => x.Action).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.PicName).MaximumLength(150);
        RuleFor(x => x.Progress).InclusiveBetween(0, 100);
    }
}

internal sealed class UpdateCapCommandHandler : IRequestHandler<UpdateCapCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;

    public UpdateCapCommandHandler(IApplicationDbContext db, IAuditService audit, ICurrentUserService currentUser)
    {
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateCapCommand request, CancellationToken cancellationToken)
    {
        var scope = await CurrentUserAccess.ResolveAsync(_db, _currentUser, cancellationToken);

        var cap = await _db.CorrectiveActions
            .Include(c => c.Finding)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Rencana tindak lanjut tidak ditemukan.");

        CurrentUserAccess.EnsureCanAccessFinding(scope, cap.Finding?.DepartmentId);

        if (cap.Status is not (CorrectiveActionStatus.Open or CorrectiveActionStatus.InProgress))
            throw new InvalidOperationException(
                $"CAP tidak dapat diubah pada status '{cap.Status}'. Perubahan hanya saat Open atau In Progress.");

        cap.Action = request.Action.Trim();
        cap.PicName = request.PicName?.Trim();
        cap.TargetDate = request.TargetDate;
        cap.Progress = request.Progress;

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Cap.Updated", nameof(CorrectiveAction), cap.Id.ToString(),
            newValues: $"{cap.Action} | {cap.Progress}%",
            cancellationToken: cancellationToken);
    }
}