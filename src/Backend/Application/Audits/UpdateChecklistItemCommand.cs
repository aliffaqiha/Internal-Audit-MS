using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Audits;

public sealed record UpdateChecklistItemCommand(
    Guid AuditPlanId,
    Guid ItemId,
    ChecklistItemStatus Status,
    string? Note) : IRequest;

public sealed class UpdateChecklistItemCommandValidator : AbstractValidator<UpdateChecklistItemCommand>
{
    public UpdateChecklistItemCommandValidator()
    {
        RuleFor(x => x.AuditPlanId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

internal sealed class UpdateChecklistItemCommandHandler : IRequestHandler<UpdateChecklistItemCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public UpdateChecklistItemCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task Handle(UpdateChecklistItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _db.AuditChecklistItems
            .Include(i => i.AuditPlan)
            .FirstOrDefaultAsync(
                i => i.Id == request.ItemId && i.AuditPlanId == request.AuditPlanId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Item checklist tidak ditemukan.");

        if (item.AuditPlan?.Status != AuditPlanStatus.InProgress)
            throw new InvalidOperationException(
                "Checklist hanya dapat dijalankan saat rencana audit berstatus In Progress.");

        var oldStatus = item.Status;
        item.Status = request.Status;
        item.Note = request.Note?.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("ChecklistItem.Updated", nameof(AuditChecklistItem), item.Id.ToString(),
            oldValues: oldStatus.ToString(), newValues: request.Status.ToString(),
            cancellationToken: cancellationToken);
    }
}