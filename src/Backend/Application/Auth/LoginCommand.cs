using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Auth;

public sealed record LoginCommand(string EmailOrUsername, string Password) : IRequest<AuthResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.EmailOrUsername).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty();
    }
}

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);

    /// <summary>
    /// A real PBKDF2 (ASP.NET Identity format) hash used to equalize response time when
    /// the account does not exist, preventing user enumeration via timing.
    /// </summary>
    private const string TimingEqualizationHash =
        "AQEAAACghgEAEAAAAN+AioocHSFS75qQc3eOIj1O/9UmYe4pkQPX7NC2UBPEBf46YwK8iL44AV8o6A44Qg==";

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public LoginCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        ITokenProvider tokenProvider,
        ICurrentUserService currentUser,
        IAuditService audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenProvider = tokenProvider;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalized = RoleConstants.Normalize(request.EmailOrUsername);

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(
                u => u.NormalizedUsername == normalized || u.NormalizedEmail == normalized,
                cancellationToken);

        if (user is null)
        {
            // Run a real PBKDF2 verify so timing does not reveal whether the account exists.
            _passwordHasher.Verify(request.Password, TimingEqualizationHash);

            await _audit.LogAsync("Login.Failed", nameof(User), null, newValues: "Invalid credentials",
                cancellationToken: cancellationToken);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc > DateTimeOffset.UtcNow)
        {
            await _audit.LogAsync("Login.Failed", nameof(User), user.Id.ToString(),
                newValues: "Account locked", cancellationToken: cancellationToken);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockoutEndUtc = DateTimeOffset.UtcNow.Add(LockoutDuration);
                user.FailedLoginCount = 0;
            }

            await _db.SaveChangesAsync(cancellationToken);
            await _audit.LogAsync("Login.Failed", nameof(User), user.Id.ToString(),
                newValues: "Invalid credentials", cancellationToken: cancellationToken);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            await _audit.LogAsync("Login.Failed", nameof(User), user.Id.ToString(),
                newValues: "Disabled account", cancellationToken: cancellationToken);
            throw new UnauthorizedAccessException("Account is disabled.");
        }

        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        user.LastLoginAt = DateTimeOffset.UtcNow;

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var access = _tokenProvider.CreateAccessToken(user, roles);
        var refresh = _tokenProvider.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            User = user,
            TokenHash = TokenHasher.Hash(refresh),
            JwtId = access.JwtId,
            ExpiresAt = DateTime.UtcNow.Add(RefreshLifetime),
            CreatedByIp = _currentUser.IpAddress
        });

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Login.Success", nameof(User), user.Id.ToString(),
            newValues: string.Join(", ", roles), cancellationToken: cancellationToken);

        return new AuthResponse(
            access.Token,
            access.ExpiresAt,
            refresh,
            new AuthUserResponse(user.Id, user.Email, user.FullName, roles, user.MustChangePassword));
    }
}

internal static class TokenHasher
{
    public static string Hash(string secret)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))).ToLowerInvariant();
}