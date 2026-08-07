using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Departments;

public sealed record UpdateDepartmentCommand(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive) : IRequest;

public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300);
    }
}

internal sealed class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public UpdateDepartmentCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Department not found.");

        var duplicate = await _db.Departments
            .AnyAsync(d => d.Id != request.Id && d.Name.ToUpper() == request.Name.ToUpperInvariant(),
                cancellationToken);
        if (duplicate)
            throw new InvalidOperationException($"Department '{request.Name}' already exists.");

        var oldName = department.Name;
        department.Name = request.Name;
        department.Description = request.Description;
        department.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Department.Updated", nameof(Department), department.Id.ToString(),
            oldValues: oldName, newValues: request.Name, cancellationToken: cancellationToken);
    }
}