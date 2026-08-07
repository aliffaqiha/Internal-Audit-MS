using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Findings;

public sealed record DeleteFindingCommand(Guid Id) : IRequest;

public sealed class DeleteFindingCommandValidator : AbstractValidator<DeleteFindingCommand>
{
    public DeleteFindingCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

internal sealed class DeleteFindingCommandHandler : IRequestHandler<DeleteFindingCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly IObjectStorageService _storage;

    public DeleteFindingCommandHandler(
        IApplicationDbContext db, IAuditService audit, IObjectStorageService storage)
    {
        _db = db;
        _audit = audit;
        _storage = storage;
    }

    public async Task Handle(DeleteFindingCommand request, CancellationToken cancellationToken)
    {
        var finding = await _db.Findings
            .Include(f => f.Evidences)
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Temuan tidak ditemukan.");

        foreach (var evidence in finding.Evidences)
            await _storage.DeleteAsync(evidence.StoredObjectName, cancellationToken);

        _db.Findings.Remove(finding);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Finding.Deleted", nameof(Finding), finding.Id.ToString(),
            oldValues: finding.Title, cancellationToken: cancellationToken);
    }
}