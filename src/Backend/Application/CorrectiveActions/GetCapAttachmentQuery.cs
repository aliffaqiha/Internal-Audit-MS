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

    public GetCapAttachmentQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CapAttachmentDownloadDto> Handle(GetCapAttachmentQuery request, CancellationToken cancellationToken)
    {
        var cap = await _db.CorrectiveActions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
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