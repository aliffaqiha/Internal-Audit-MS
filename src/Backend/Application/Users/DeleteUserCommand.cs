using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Users;

public sealed record DeleteUserCommand(Guid Id) : IRequest;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

internal sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public DeleteUserCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (request.Id == _currentUser.UserId)
            throw new InvalidOperationException("You cannot delete your own account.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        // Soft delete: deactivate so related records (audit, tokens) remain valid.
        user.IsActive = false;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("User.Deleted", nameof(User), user.Id.ToString(),
            oldValues: user.Username, cancellationToken: cancellationToken);
    }
}