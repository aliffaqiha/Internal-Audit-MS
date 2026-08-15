namespace IAMS.Application.Auth;

public sealed record AuthUserResponse(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    bool MustChangePassword);

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    AuthUserResponse User);