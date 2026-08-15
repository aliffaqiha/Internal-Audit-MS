namespace IAMS.Infrastructure.Common;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>
    /// When true the refresh token is issued as an httpOnly cookie instead of
    /// being returned in the response body (XSS-resistant).
    /// </summary>
    public bool RefreshTokenCookie { get; set; } = true;

    /// <summary>
    /// Marks the refresh cookie Secure (HTTPS-only). Keep true unless the app is
    /// intentionally served over plain HTTP (e.g. local development).
    /// </summary>
    public bool SecureCookie { get; set; } = true;
}