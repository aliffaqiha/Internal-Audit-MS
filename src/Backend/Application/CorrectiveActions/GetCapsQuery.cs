using IAMS.Application.Common;
using IAMS.Application.Common.DataScoping;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.CorrectiveActions;

public sealed record GetCapsQuery(
    CorrectiveActionStatus? Status = null,
    Guid? FindingId = null,
    Guid? DepartmentId = null,
    int Page = 1,
    int PageSize = Pagination.DefaultPageSize) : IRequest<PagedResult<CorrectiveActionDto>>;

internal sealed class GetCapsQueryHandler : IRequestHandler<GetCapsQuery, PagedResult<CorrectiveActionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCapsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<CorrectiveActionDto>> Handle(GetCapsQuery request, CancellationToken cancellationToken)
    {
        var scope = await CurrentUserAccess.ResolveAsync(_db, _currentUser, cancellationToken);

        var query = _db.CorrectiveActions
            .Include(c => c.Finding)
            .AsNoTracking()
            .RestrictCaps(scope);

        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status);
        if (request.FindingId.HasValue)
            query = query.Where(c => c.FindingId == request.FindingId);
        if (request.DepartmentId.HasValue)
            query = query.Where(c => c.Finding != null && c.Finding.DepartmentId == request.DepartmentId);

        var caps = query
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
                    : null));

        return await caps.ToPagedAsync(request.Page, request.PageSize, cancellationToken);
    }
}
