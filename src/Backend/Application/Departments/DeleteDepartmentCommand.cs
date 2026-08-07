using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Departments;

public sealed record DeleteDepartmentCommand(Guid Id) : IRequest;

public sealed class DeleteDepartmentCommandValidator : AbstractValidator<DeleteDepartmentCommand>
{
    public DeleteDepartmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

internal sealed class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public DeleteDepartmentCommandHandler(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _db.Departments
            .Include(d => d.Users)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Department not found.");

        if (department.Users.Count != 0)
            throw new InvalidOperationException(
                "Department still has assigned users. Reassign or deactivate them first.");

        _db.Departments.Remove(department);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Department.Deleted", nameof(Department), department.Id.ToString(),
            oldValues: department.Name, cancellationToken: cancellationToken);
    }
}