using IAMS.Application.Common.Interfaces;

namespace IAMS.Infrastructure.Emails;

/// <summary>
/// Async email sink: enqueues the message and returns immediately; the background
/// worker performs the actual delivery (SMTP when configured, otherwise log).
/// </summary>
public sealed class QueuedEmailService : IEmailService
{
    private readonly IEmailQueue _queue;

    public QueuedEmailService(IEmailQueue queue) => _queue = queue;

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _queue.Enqueue(new EmailMessage(to, subject, body));
        return Task.CompletedTask;
    }
}