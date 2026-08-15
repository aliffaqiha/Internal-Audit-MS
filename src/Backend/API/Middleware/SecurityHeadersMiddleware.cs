using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace IAMS.Api.Middleware;

/// <summary>
/// Applies hardening HTTP response headers. A strict CSP is skipped for the
/// Swagger/OpenAPI UIs because they rely on inline scripts and styles.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        var isApiDocumentation = context.Request.Path.StartsWithSegments("/swagger")
            || context.Request.Path.StartsWithSegments("/openapi");
        if (!isApiDocumentation)
        {
            headers["Content-Security-Policy"] =
                "default-src 'none'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; " +
                "connect-src 'self' ws: wss:; " +
                "font-src 'self'; " +
                "object-src 'none'; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self'";
        }

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
