using FluentValidation;
using IAMS.Application.Common.Exceptions;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AppValidationException = IAMS.Application.Common.Exceptions.ValidationException;

namespace IAMS.Application.Findings;

public sealed record AddEvidenceCommand(
    Guid FindingId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : IRequest<Guid>;

public sealed class AddEvidenceCommandValidator : AbstractValidator<AddEvidenceCommand>
{
    public AddEvidenceCommandValidator()
    {
        RuleFor(x => x.FindingId).NotEmpty();
        RuleFor(x => x.Content).NotNull();
    }
}

internal sealed class AddEvidenceCommandHandler : IRequestHandler<AddEvidenceCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly IObjectStorageService _storage;

    public AddEvidenceCommandHandler(
        IApplicationDbContext db, IAuditService audit, IObjectStorageService storage)
    {
        _db = db;
        _audit = audit;
        _storage = storage;
    }

    public async Task<Guid> Handle(AddEvidenceCommand request, CancellationToken cancellationToken)
    {
        var finding = await _db.Findings
            .Include(f => f.Evidences)
            .FirstOrDefaultAsync(f => f.Id == request.FindingId, cancellationToken)
            ?? throw new KeyNotFoundException("Temuan tidak ditemukan.");

        if (!EvidenceFileRules.IsContentTypeAllowed(request.ContentType))
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                ["Content"] = new[] { "Tipe file tidak diizinkan. Gunakan PDF, gambar, Excel, atau Word." }
            });

        if (request.SizeBytes > EvidenceFileRules.MaxSizeBytes)
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                ["Content"] = new[] { $"Ukuran file maksimal {EvidenceFileRules.MaxSizeBytes / (1024 * 1024)} MB." }
            });

        var safeName = EvidenceFileRules.SanitizeFileName(request.OriginalFileName);
        var version = finding.Evidences.Count + 1;
        var ext = EvidenceFileRules.ExtensionFor(request.ContentType) ?? ".bin";
        var objectName = $"findings/{finding.Id}/{Guid.NewGuid():N}{ext}";

        await _storage.UploadAsync(objectName, request.Content, request.ContentType, cancellationToken);

        var evidence = new FindingEvidence
        {
            FindingId = finding.Id,
            OriginalFileName = safeName,
            StoredObjectName = objectName,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            Version = version
        };

        _db.FindingEvidences.Add(evidence);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Evidence.Uploaded", nameof(FindingEvidence), evidence.Id.ToString(),
            oldValues: finding.Id.ToString(),
            newValues: $"{safeName} v{version}",
            cancellationToken: cancellationToken);

        return evidence.Id;
    }
}