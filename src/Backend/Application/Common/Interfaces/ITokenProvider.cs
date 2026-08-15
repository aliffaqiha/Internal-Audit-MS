using IAMS.Domain.Entities;

namespace IAMS.Application.Common.Interfaces;

public record AccessTokenInfo(string Token, DateTime ExpiresAt, Guid JwtId);

/// <summary>Creates signed access JWTs and opaque refresh secrets.</summary>
public interface ITokenProvider
{
    AccessTokenInfo CreateAccessToken(User user, IReadOnlyList<string> roles);

    /// <summary>
    /// Creates a short-lived JWT used only to authenticate a SignalR connection.
    /// It carries no role/email claims and expires quickly, so a token captured
    /// from the WebSocket query string is of limited value.
    /// </summary>
    AccessTokenInfo CreateSignalRToken(Guid userId, string username);

    string CreateRefreshToken();
    string HashSecret(string secret);
}