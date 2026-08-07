using FluentValidation;
using IAMS.Application.Common.Exceptions;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using IAMS.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AppValidationException = IAMS.Application.Common.Exceptions.ValidationException;

namespace IAMS.Application.Findings;

public sealed record CreateFindingCommand(
    string Title,
    string? Description,
    Guid? DepartmentId,
    RiskLevel RiskLevel,
    string? Category,
    string? Recommendation,
    DateTimeOffset? DueDate,
    Guid? AuditPlanId) : IRequest<Guid>;

public sealed class CreateFindingCommandValidator : AbstractValidator<CreateFindingCommand>
{
    public CreateFindingCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Recommendation).MaximumLength(2000);
        RuleFor(x => x.RiskLevel).IsInEnum();
    }
}

internal sealed class CreateFindingCommandHandler : IRequestHandler<CreateFindingCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly IPublisher _publisher;

    public CreateFindingCommandHandler(IApplicationDbContext db, IAuditService audit, IPublisher publisher)
    {
        _db = db;
        _audit = audit;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreateFindingCommand request, CancellationToken cancellationToken)
    {
        if (request.DepartmentId.HasValue
            && !await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId, cancellationToken))
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                [nameof(request.DepartmentId)] = new[] { "Departemen tidak ditemukan." }
            });
        }

        if (request.AuditPlanId.HasValue
            && !await _db.AuditPlans.AnyAsync(p => p.Id == request.AuditPlanId, cancellationToken))
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                [nameof(request.AuditPlanId)] = new[] { "Rencana audit tidak ditemukan." }
            });
        }

        var finding = new Finding
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            DepartmentId = request.DepartmentId,
            RiskLevel = request.RiskLevel,
            Category = request.Category?.Trim(),
            Recommendation = request.Recommendation?.Trim(),
            DueDate = request.DueDate,
            AuditPlanId = request.AuditPlanId
        };

        _db.Findings.Add(finding);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Finding.Created", nameof(Finding), finding.Id.ToString(),
            newValues: finding.Title, cancellationToken: cancellationToken);

        await _publisher.Publish(new FindingCreatedEvent(
            finding.Id, finding.Title, finding.RiskLevel, finding.DueDate, finding.DepartmentId), cancellationToken);

        return finding.Id;
    }
}