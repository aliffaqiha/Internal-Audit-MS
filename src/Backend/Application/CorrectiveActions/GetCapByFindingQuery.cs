using IAMS.Application.Common.DataScoping;
using IAMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.CorrectiveActions;

public sealed record GetCapByFindingQuery(Guid FindingId) : IRequest<CorrectiveActionDto?>;

internal sealed class GetCapByFindingQueryHandler : IRequestHandler<GetCapByFindingQuery, CorrectiveActionDto?>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCapByFindingQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CorrectiveActionDto?> Handle(GetCapByFindingQuery request, CancellationToken cancellationToken)
    {
        var scope = await CurrentUserAccess.ResolveAsync(_db, _currentUser, cancellationToken);

        var cap = await _db.CorrectiveActions
            .AsNoTracking()
            .Include(c => c.Finding)
            .Where(c => c.FindingId == request.FindingId)
            .RestrictCaps(scope)
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
            .FirstOrDefaultAsync(cancellationToken);

        return cap;
    }
}
