using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using ValidationException = IAMS.Application.Common.Exceptions.ValidationException;

namespace IAMS.Api.Middleware;

/// <summary>
/// Central exception handler that maps application exceptions to proper HTTP responses.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, payload) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                new { message = "Validation failed.", errors = ve.Errors }),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                new { message = "Unauthorized." } as object),

            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                new { message = exception.Message } as object),

            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                new { message = exception.Message } as object),

            _ => (
                StatusCodes.Status500InternalServerError,
                new { message = "An unexpected error occurred." } as object)
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(payload, context.RequestAborted);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}