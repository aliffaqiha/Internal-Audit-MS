using IAMS.Application.Common.DataScoping;
using IAMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.CorrectiveActions;

public sealed record CapAttachmentDownloadDto(
    string StoredObjectName,
    string FileName,
    string ContentType,
    long SizeBytes);

public sealed record GetCapAttachmentQuery(Guid Id) : IRequest<CapAttachmentDownloadDto>;

internal sealed class GetCapAttachmentQueryHandler : IRequestHandler<GetCapAttachmentQuery, CapAttachmentDownloadDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCapAttachmentQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CapAttachmentDownloadDto> Handle(GetCapAttachmentQuery request, CancellationToken cancellationToken)
    {
        var scope = await CurrentUserAccess.ResolveAsync(_db, _currentUser, cancellationToken);

        var cap = await _db.CorrectiveActions
            .AsNoTracking()
            .Include(c => c.Finding)
            .Where(c => c.Id == request.Id)
            .RestrictCaps(scope)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Rencana tindak lanjut tidak ditemukan.");

        if (cap.AttachmentObjectName == null)
            throw new KeyNotFoundException("CAP ini belum memiliki lampiran.");

        return new CapAttachmentDownloadDto(
            cap.AttachmentObjectName,
            cap.AttachmentFileName ?? "attachment.bin",
            cap.AttachmentContentType ?? "application/octet-stream",
            cap.AttachmentSizeBytes ?? 0);
    }
}
