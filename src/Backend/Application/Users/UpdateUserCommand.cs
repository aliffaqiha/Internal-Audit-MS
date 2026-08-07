using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Users;

public sealed record UpdateUserCommand(
    Guid Id,
    string Email,
    string FullName,
    Guid? DepartmentId,
    IReadOnlyList<Guid> RoleIds,
    bool IsActive,
    string? NewPassword) : IRequest;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RoleIds).NotNull().Must(r => r.Count != 0).WithMessage("At least one role is required.");
        When(x => !string.IsNullOrEmpty(x.NewPassword), () =>
        {
            RuleFor(x => x.NewPassword!).MinimumLength(8)
                .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain a digit.");
        });
    }
}

internal sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public UpdateUserCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUser,
        IAuditService audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        await CreateUserCommandHandler.EnsureUniqueIdentityAsync(
            _db, user.Username, request.Email, user.Id, cancellationToken);

        if (!request.IsActive && request.Id == _currentUser.UserId)
            throw new InvalidOperationException("You cannot deactivate your own account.");

        if (!request.IsActive)
        {
            var adminRoleId = await _db.Roles
                .Where(r => r.NormalizedName == RoleConstants.Normalize(RoleConstants.Administrator))
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var wasAdmin = adminRoleId != Guid.Empty && user.UserRoles.Any(ur => ur.RoleId == adminRoleId);

            var remainingAdmins = await _db.Users
                .Where(u => u.Id != user.Id && u.IsActive)
                .SelectMany(u => u.UserRoles)
                .Where(ur => ur.RoleId == adminRoleId)
                .CountAsync(cancellationToken);

            if (wasAdmin && remainingAdmins == 0)
                throw new InvalidOperationException("Cannot deactivate the last active administrator.");
        }

        var oldValues = $"{user.Email}|{user.FullName}|active={user.IsActive}";

        user.Email = request.Email;
        user.NormalizedEmail = request.Email.ToUpperInvariant();
        user.FullName = request.FullName;
        user.DepartmentId = request.DepartmentId;
        user.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
            user.MustChangePassword = false;
        }

        SyncRoles(user, request.RoleIds);

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("User.Updated", nameof(User), user.Id.ToString(),
            oldValues: oldValues, newValues: $"{user.Email}|{user.FullName}|active={user.IsActive}",
            cancellationToken: cancellationToken);
    }

    private void SyncRoles(User user, IReadOnlyList<Guid> roleIds)
    {
        var target = roleIds.Distinct().ToHashSet();
        var current = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();

        foreach (var roleId in current.Except(target))
        {
            var userRole = user.UserRoles.First(ur => ur.RoleId == roleId);
            user.UserRoles.Remove(userRole);
        }

        foreach (var roleId in target.Except(current))
            _db.UserRoles.Add(new UserRole { User = user, RoleId = roleId });
    }
}