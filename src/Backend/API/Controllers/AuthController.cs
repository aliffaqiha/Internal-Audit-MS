using IAMS.Application.Auth;
using IAMS.Infrastructure.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace IAMS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string RefreshCookieName = "iams.refreshToken";
    private const string RefreshCookiePath = "/api/auth";

    private readonly ISender _sender;
    private readonly JwtOptions _jwt;

    public AuthController(ISender sender, IOptions<JwtOptions> jwt)
    {
        _sender = sender;
        _jwt = jwt.Value;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return Ok(WithRefreshCookie(result));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenCommand? command, CancellationToken ct)
    {
        var token = ResolveRefreshToken(command?.RefreshToken);
        var result = await _sender.Send(new RefreshTokenCommand(token), ct);
        return Ok(WithRefreshCookie(result));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand? command, CancellationToken ct)
    {
        var token = ResolveRefreshToken(command?.RefreshToken);
        if (!string.IsNullOrWhiteSpace(token))
            await _sender.Send(new LogoutCommand(token), ct);

        ClearRefreshCookie();
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, CancellationToken ct)
    {
        await _sender.Send(command, ct);
        return Accepted();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken ct)
    {
        await _sender.Send(command, ct);
        ClearRefreshCookie();
        return NoContent();
    }

    /// <summary>
    /// When cookie mode is enabled the refresh token is moved from the response
    /// body into an httpOnly cookie scoped to the auth endpoints.
    /// </summary>
    private AuthResponse WithRefreshCookie(AuthResponse result)
    {
        if (!_jwt.RefreshTokenCookie || string.IsNullOrEmpty(result.RefreshToken))
            return result;

        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = RefreshCookiePath,
            MaxAge = TimeSpan.FromDays(_jwt.RefreshTokenDays),
            IsEssential = true
        };
        Response.Cookies.Append(RefreshCookieName, result.RefreshToken, options);

        return result with { RefreshToken = null! };
    }

    /// <summary>Prefers the httpOnly cookie, falling back to a body-provided token.</summary>
    private string ResolveRefreshToken(string? bodyToken)
    {
        var cookieToken = Request.Cookies[RefreshCookieName];
        return !string.IsNullOrWhiteSpace(cookieToken) ? cookieToken : (bodyToken ?? string.Empty);
    }

    private void ClearRefreshCookie()
    {
        if (Request.Cookies.ContainsKey(RefreshCookieName))
        {
            Response.Cookies.Delete(RefreshCookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = RefreshCookiePath
            });
        }
    }
}
