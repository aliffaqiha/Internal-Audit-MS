using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Application.Common.Exceptions;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AppValidationException = IAMS.Application.Common.Exceptions.ValidationException;

namespace IAMS.Application.Audits;

public sealed record AuditChecklistItemInput(string Question, string? Category, bool IsRequired = true);

public sealed record AuditAssignmentInput(Guid UserId, string? RoleInPlan);

public sealed record CreateAuditPlanCommand(
    string Title,
    string? Objective,
    string? Scope,
    string? Standard,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    Guid? DepartmentId,
    IReadOnlyList<AuditAssignmentInput> Assignments,
    IReadOnlyList<AuditChecklistItemInput> ChecklistItems) : IRequest<Guid>;

public sealed class CreateAuditPlanCommandValidator : AbstractValidator<CreateAuditPlanCommand>
{
    public const string TitleMissing = "Judul rencana audit wajib diisi.";

    public CreateAuditPlanCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage(TitleMissing).MaximumLength(200);
        RuleFor(x => x.Objective).MaximumLength(1000);
        RuleFor(x => x.Scope).MaximumLength(1000);
        RuleFor(x => x.Standard).MaximumLength(100);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("Tanggal selesai harus setelah tanggal mulai.");
        RuleFor(x => x.Assignments).NotNull();
        RuleFor(x => x.ChecklistItems).NotNull();
    }
}

internal sealed class CreateAuditPlanCommandHandler : IRequestHandler<CreateAuditPlanCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public CreateAuditPlanCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<Guid> Handle(CreateAuditPlanCommand request, CancellationToken cancellationToken)
    {
        if (request.Assignments.Count != 0)
        {
            var userIds = request.Assignments.Select(a => a.UserId).Distinct().ToHashSet();
            var existing = await _db.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            if (existing.Count != userIds.Count)
                throw new AppValidationException(new Dictionary<string, string[]>
                {
                    [nameof(request.Assignments)] = new[] { "Satu atau lebih anggota tim tidak ditemukan." }
                });
        }

        if (request.DepartmentId.HasValue)
        {
            var depExists = await _db.Departments.AnyAsync(
                d => d.Id == request.DepartmentId, cancellationToken);
            if (!depExists)
                throw new AppValidationException(new Dictionary<string, string[]>
                {
                    [nameof(request.DepartmentId)] = new[] { "Departemen yang diaudit tidak ditemukan." }
                });
        }

        var plan = new AuditPlan
        {
            Title = request.Title.Trim(),
            Objective = request.Objective?.Trim(),
            Scope = request.Scope?.Trim(),
            Standard = request.Standard?.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DepartmentId = request.DepartmentId,
            Status = AuditPlanStatus.Draft
        };

        foreach (var assignment in request.Assignments)
        {
            plan.Assignments.Add(new AuditAssignment
            {
                UserId = assignment.UserId,
                RoleInPlan = assignment.RoleInPlan?.Trim()
            });
        }

        var requestedItems = request.ChecklistItems
            .Select(i => new { Question = i.Question.Trim(), i.Category, i.IsRequired })
            .ToList();

        if (requestedItems.Count == 0)
            requestedItems = StandardChecklistTemplates.ForStandard(request.Standard)
                .Select(i => new
                {
                    Question = i.Question,
                    Category = i.Category,
                    IsRequired = true
                })
                .ToList();

        foreach (var item in requestedItems)
        {
            if (string.IsNullOrWhiteSpace(item.Question))
                continue;
            plan.ChecklistItems.Add(new AuditChecklistItem
            {
                Question = item.Question,
                Category = item.Category?.Trim(),
                IsRequired = item.IsRequired
            });
        }

        _db.AuditPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("AuditPlan.Created", nameof(AuditPlan), plan.Id.ToString(),
            newValues: plan.Title, cancellationToken: cancellationToken);

        return plan.Id;
    }
}