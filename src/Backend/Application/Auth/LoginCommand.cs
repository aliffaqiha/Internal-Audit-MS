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

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await _audit.LogAsync("Login.Failed", nameof(User), null, newValues: "Invalid credentials",
                cancellationToken: cancellationToken);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            await _audit.LogAsync("Login.Failed", nameof(User), user.Id.ToString(),
                newValues: "Disabled account", cancellationToken: cancellationToken);
            throw new UnauthorizedAccessException("Account is disabled.");
        }

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
            new AuthUserResponse(user.Id, user.Email, user.FullName, roles));
    }
}

internal static class TokenHasher
{
    public static string Hash(string secret)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))).ToLowerInvariant();
}