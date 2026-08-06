using FluentValidation;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Auth;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

internal sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);

    private readonly IApplicationDbContext _db;
    private readonly ITokenProvider _tokenProvider;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public RefreshTokenCommandHandler(
        IApplicationDbContext db,
        ITokenProvider tokenProvider,
        ICurrentUserService currentUser,
        IAuditService audit)
    {
        _db = db;
        _tokenProvider = tokenProvider;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = TokenHasher.Hash(request.RefreshToken);

        var token = await _db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!token.IsActive)
            throw new UnauthorizedAccessException("Refresh token is no longer active.");

        var user = token.User;
        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        // Rotation: revoke the old token and issue a new one.
        token.RevokedAt = DateTimeOffset.UtcNow;
        token.ReplacedByToken = Guid.NewGuid().ToString("N")[..16];

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
        await _audit.LogAsync("Token.Refreshed", nameof(RefreshToken), token.Id.ToString(),
            cancellationToken: cancellationToken);

        return new AuthResponse(
            access.Token,
            access.ExpiresAt,
            refresh,
            new AuthUserResponse(user.Id, user.Email, user.FullName, roles));
    }
}