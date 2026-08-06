using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Auth;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

internal sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _audit;

    public ResetPasswordCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IAuditService audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _audit = audit;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var resetToken = await _db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == TokenHasher.Hash(request.Token), cancellationToken);

        if (resetToken is null || resetToken.UsedAt.HasValue || resetToken.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Password reset token is invalid or expired.");

        resetToken.User.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        resetToken.User.MustChangePassword = false;
        resetToken.UsedAt = DateTimeOffset.UtcNow;

        // Invalidate all existing refresh tokens after a password reset.
        var refreshTokens = await _db.RefreshTokens
            .Where(t => t.UserId == resetToken.UserId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var t in refreshTokens)
            t.RevokedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Password.Reset", nameof(User), resetToken.UserId.ToString(),
            cancellationToken: cancellationToken);
    }
}