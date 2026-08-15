using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Auth;

public sealed record ForgotPasswordCommand(string Email) : IRequest;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

internal sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private static readonly TimeSpan ResetLifetime = TimeSpan.FromHours(1);

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;
    private readonly IAppSettings _appSettings;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IEmailService email,
        IAuditService audit,
        IAppSettings appSettings)
    {
        _db = db;
        _currentUser = currentUser;
        _email = email;
        _audit = audit;
        _appSettings = appSettings;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Always succeed to avoid leaking whether an account exists.
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.ToUpperInvariant(),
                cancellationToken);

        if (user is null)
            return;

        var rawToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            User = user,
            TokenHash = TokenHasher.Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.Add(ResetLifetime),
            CreatedByIp = _currentUser.IpAddress
        });

        await _db.SaveChangesAsync(cancellationToken);

        var resetUrl = $"{_appSettings.ClientBaseUrl}/reset-password?token={rawToken}";
        await _email.SendAsync(
            user.Email,
            "IAMS - Password Reset",
            $"Reset your password using this link: {resetUrl}",
            cancellationToken);

        await _audit.LogAsync("Password.ResetRequested", nameof(User), user.Id.ToString(),
            cancellationToken: cancellationToken);
    }
}