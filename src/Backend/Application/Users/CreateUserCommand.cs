using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Users;

public sealed record CreateUserCommand(
    string Username,
    string Email,
    string FullName,
    string Password,
    Guid? DepartmentId,
    IReadOnlyList<Guid> RoleIds,
    bool IsActive) : IRequest<Guid>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50)
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Username contains invalid characters.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
        RuleFor(x => x.RoleIds).NotNull().Must(r => r.Count != 0).WithMessage("At least one role is required.");
    }
}

internal sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _audit;

    public CreateUserCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IAuditService audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _audit = audit;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
await CreateUserCommandHandler.EnsureUniqueIdentityAsync(
            _db, request.Username, request.Email, null, cancellationToken);

        var roleIds = await _db.Roles
            .Where(r => request.RoleIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (roleIds.Count != request.RoleIds.Distinct().Count())
            throw new InvalidOperationException("One or more roles do not exist.");

        var user = new User
        {
            Username = request.Username,
            NormalizedUsername = request.Username.ToUpperInvariant(),
            Email = request.Email,
            NormalizedEmail = request.Email.ToUpperInvariant(),
            FullName = request.FullName,
            PasswordHash = _passwordHasher.Hash(request.Password),
            DepartmentId = request.DepartmentId,
            IsActive = request.IsActive,
            MustChangePassword = false
        };

        _db.Users.Add(user);
        foreach (var roleId in roleIds)
            _db.UserRoles.Add(new UserRole { User = user, RoleId = roleId });

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("User.Created", nameof(User), user.Id.ToString(),
            newValues: request.Username, cancellationToken: cancellationToken);

        return user.Id;
    }

    internal static async Task EnsureUniqueIdentityAsync(
        IApplicationDbContext db,
        string username,
        string email,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var dup = await db.Users.AnyAsync(
            u => (u.NormalizedUsername == username.ToUpperInvariant()
                  || u.NormalizedEmail == email.ToUpperInvariant())
                 && u.Id != excludeId,
            cancellationToken);

        if (dup)
            throw new InvalidOperationException("Username or email is already in use.");
    }
}