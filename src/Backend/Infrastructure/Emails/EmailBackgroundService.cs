using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IAMS.Infrastructure.Emails;

/// <summary>Background worker that drains the in-memory email queue.</summary>
public sealed class EmailBackgroundService : BackgroundService
{
    private readonly IEmailQueue _queue;
    private readonly IEmailDispatcher _dispatcher;
    private readonly ILogger<EmailBackgroundService> _logger;

    public EmailBackgroundService(
        IEmailQueue queue,
        IEmailDispatcher dispatcher,
        ILogger<EmailBackgroundService> logger)
    {
        _queue = queue;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _dispatcher.DeliverAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deliver email to {To}: {Subject}", message.To, message.Subject);
            }
        }
    }
}