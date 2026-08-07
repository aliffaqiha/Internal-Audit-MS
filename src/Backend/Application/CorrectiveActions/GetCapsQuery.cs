using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.CorrectiveActions;

public sealed record GetCapsQuery(
    CorrectiveActionStatus? Status = null,
    Guid? FindingId = null,
    Guid? DepartmentId = null) : IRequest<IReadOnlyList<CorrectiveActionDto>>;

internal sealed class GetCapsQueryHandler : IRequestHandler<GetCapsQuery, IReadOnlyList<CorrectiveActionDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCapsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CorrectiveActionDto>> Handle(GetCapsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.CorrectiveActions
            .Include(c => c.Finding)
            .AsNoTracking();

        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status);
        if (request.FindingId.HasValue)
            query = query.Where(c => c.FindingId == request.FindingId);
        if (request.DepartmentId.HasValue)
            query = query.Where(c => c.Finding != null && c.Finding.DepartmentId == request.DepartmentId);

        var caps = await query
            .OrderBy(c => c.Status)
            .ThenByDescending(c => c.CreatedAt)
            .Select(c => new CorrectiveActionDto(
                c.Id,
                c.FindingId,
                c.Finding != null ? c.Finding.Title : string.Empty,
                c.Action,
                c.PicName,
                c.TargetDate,
                c.Progress,
                c.Status,
                c.RejectionReason,
                c.VerificationNote,
                c.VerifiedAt,
                c.AttachmentObjectName != null
                    ? new CapAttachmentDto(c.AttachmentFileName, c.AttachmentContentType, c.AttachmentSizeBytes, c.AttachmentUploadedAt)
                    : null))
            .ToListAsync(cancellationToken);

        return caps;
    }
}