using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Departments;

public sealed record CreateDepartmentCommand(string Name, string? Description) : IRequest<Guid>;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300);
    }
}

internal sealed class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public CreateDepartmentCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<Guid> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var exists = await _db.Departments
            .AnyAsync(d => d.Name.ToUpper() == request.Name.ToUpperInvariant(), cancellationToken);
        if (exists)
            throw new InvalidOperationException($"Department '{request.Name}' already exists.");

        var department = new Department
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        _db.Departments.Add(department);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Department.Created", nameof(Department), department.Id.ToString(),
            newValues: request.Name, cancellationToken: cancellationToken);

        return department.Id;
    }
}