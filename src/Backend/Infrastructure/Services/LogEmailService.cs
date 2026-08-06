using IAMS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace IAMS.Infrastructure.Services;

/// <summary>
/// Placeholder email sink that logs the message instead of sending it.
/// Replace with an SMTP/SendGrid implementation for production.
/// </summary>
public sealed class LogEmailService : IEmailService
{
    private readonly ILogger<LogEmailService> _logger;

    public LogEmailService(ILogger<LogEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email -> {To} | {Subject} | {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}