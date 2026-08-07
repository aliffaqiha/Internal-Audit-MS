using FluentValidation;
using IAMS.Application.Common.Exceptions;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AppValidationException = IAMS.Application.Common.Exceptions.ValidationException;

namespace IAMS.Application.Findings;

public sealed record UpdateFindingCommand(
    string Title,
    string? Description,
    Guid? DepartmentId,
    RiskLevel RiskLevel,
    string? Category,
    string? Recommendation,
    DateTimeOffset? DueDate,
    Guid? AuditPlanId,
    Guid Id) : IRequest;

public sealed class UpdateFindingCommandValidator : AbstractValidator<UpdateFindingCommand>
{
    public UpdateFindingCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Recommendation).MaximumLength(2000);
        RuleFor(x => x.RiskLevel).IsInEnum();
    }
}

internal sealed class UpdateFindingCommandHandler : IRequestHandler<UpdateFindingCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public UpdateFindingCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task Handle(UpdateFindingCommand request, CancellationToken cancellationToken)
    {
        var finding = await _db.Findings.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Temuan tidak ditemukan.");

        if (request.DepartmentId.HasValue
            && !await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId, cancellationToken))
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                [nameof(request.DepartmentId)] = new[] { "Departemen tidak ditemukan." }
            });

        finding.Title = request.Title.Trim();
        finding.Description = request.Description?.Trim();
        finding.DepartmentId = request.DepartmentId;
        finding.RiskLevel = request.RiskLevel;
        finding.Category = request.Category?.Trim();
        finding.Recommendation = request.Recommendation?.Trim();
        finding.DueDate = request.DueDate;
        finding.AuditPlanId = request.AuditPlanId;

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Finding.Updated", nameof(Finding), finding.Id.ToString(),
            oldValues: "before", newValues: finding.Title, cancellationToken: cancellationToken);
    }
}