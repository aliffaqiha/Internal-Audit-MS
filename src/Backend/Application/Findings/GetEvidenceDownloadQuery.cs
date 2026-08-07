using IAMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Findings;

public sealed record EvidenceDownloadDto(
    Guid EvidenceId,
    string StoredObjectName,
    string OriginalFileName,
    string ContentType,
    long SizeBytes);

public sealed record GetEvidenceDownloadQuery(Guid FindingId, Guid EvidenceId) : IRequest<EvidenceDownloadDto>;

internal sealed class GetEvidenceDownloadQueryHandler : IRequestHandler<GetEvidenceDownloadQuery, EvidenceDownloadDto>
{
    private readonly IApplicationDbContext _db;

    public GetEvidenceDownloadQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<EvidenceDownloadDto> Handle(GetEvidenceDownloadQuery request, CancellationToken cancellationToken)
    {
        var evidence = await _db.FindingEvidences
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.FindingId == request.FindingId && e.Id == request.EvidenceId, cancellationToken)
            ?? throw new KeyNotFoundException("Bukti tidak ditemukan.");

        return new EvidenceDownloadDto(
            evidence.Id,
            evidence.StoredObjectName,
            evidence.OriginalFileName,
            evidence.ContentType,
            evidence.SizeBytes);
    }
}