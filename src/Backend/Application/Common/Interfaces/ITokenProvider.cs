using IAMS.Domain.Entities;

namespace IAMS.Application.Common.Interfaces;

public record AccessTokenInfo(string Token, DateTime ExpiresAt, Guid JwtId);

/// <summary>Creates signed access JWTs and opaque refresh secrets.</summary>
public interface ITokenProvider
{
    AccessTokenInfo CreateAccessToken(User user, IReadOnlyList<string> roles);
    string CreateRefreshToken();
    string HashSecret(string secret);
}