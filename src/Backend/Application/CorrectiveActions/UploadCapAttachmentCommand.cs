using FluentValidation;
using IAMS.Application.Common.DataScoping;
using IAMS.Application.Common.Exceptions;
using IAMS.Application.Common.Interfaces;
using IAMS.Application.Findings;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AppValidationException = IAMS.Application.Common.Exceptions.ValidationException;

namespace IAMS.Application.CorrectiveActions;

public sealed record UploadCapAttachmentCommand(
    Guid CapId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : IRequest;

public sealed class UploadCapAttachmentCommandValidator : AbstractValidator<UploadCapAttachmentCommand>
{
    public UploadCapAttachmentCommandValidator()
    {
        RuleFor(x => x.CapId).NotEmpty();
        RuleFor(x => x.Content).NotNull();
    }
}

internal sealed class UploadCapAttachmentCommandHandler : IRequestHandler<UploadCapAttachmentCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly IObjectStorageService _storage;
    private readonly ICurrentUserService _currentUser;

    public UploadCapAttachmentCommandHandler(
        IApplicationDbContext db, IAuditService audit, IObjectStorageService storage, ICurrentUserService currentUser)
    {
        _db = db;
        _audit = audit;
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task Handle(UploadCapAttachmentCommand request, CancellationToken cancellationToken)
    {
        var scope = await CurrentUserAccess.ResolveAsync(_db, _currentUser, cancellationToken);

        var cap = await _db.CorrectiveActions
            .Include(c => c.Finding)
            .FirstOrDefaultAsync(c => c.Id == request.CapId, cancellationToken)
            ?? throw new KeyNotFoundException("Rencana tindak lanjut tidak ditemukan.");

        CurrentUserAccess.EnsureCanAccessFinding(scope, cap.Finding?.DepartmentId);

        if (cap.Status == CorrectiveActionStatus.Closed)
            throw new InvalidOperationException("CAP yang sudah ditutup tidak dapat melampirkan bukti baru.");

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

        var family = EvidenceFileRules.SniffFileFamily(request.Content);
        if (family == EvidenceFileRules.FileFamily.Unknown
            || !EvidenceFileRules.MatchesFamily(request.ContentType, family))
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                ["Content"] = new[] { "Isi file tidak sesuai dengan tipe yang dipilih." }
            });
        }

        var safeName = EvidenceFileRules.SanitizeFileName(request.OriginalFileName);
        var ext = EvidenceFileRules.ExtensionFor(request.ContentType) ?? ".bin";
        var objectName = $"caps/{cap.Id}/{Guid.NewGuid():N}{ext}";

        if (cap.AttachmentObjectName != null)
            await _storage.DeleteAsync(cap.AttachmentObjectName, cancellationToken);

        await _storage.UploadAsync(objectName, request.Content, request.ContentType, cancellationToken);

        cap.AttachmentObjectName = objectName;
        cap.AttachmentFileName = safeName;
        cap.AttachmentContentType = request.ContentType;
        cap.AttachmentSizeBytes = request.SizeBytes;
        cap.AttachmentUploadedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Cap.AttachmentUploaded", nameof(CorrectiveAction), cap.Id.ToString(),
            newValues: safeName, cancellationToken: cancellationToken);
    }
}