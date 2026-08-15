using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Auth;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

internal sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public ChangePasswordCommandHandler(
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

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException("Unauthorized.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Unauthorized.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Password saat ini salah.");

        if (string.Equals(request.NewPassword, request.CurrentPassword, StringComparison.Ordinal))
            throw new InvalidOperationException("Password baru harus berbeda dari password saat ini.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;

        // Changing the password invalidates every active session.
        var refreshTokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var t in refreshTokens)
            t.RevokedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Password.Changed", nameof(User), userId.ToString(),
            cancellationToken: cancellationToken);
    }
}
