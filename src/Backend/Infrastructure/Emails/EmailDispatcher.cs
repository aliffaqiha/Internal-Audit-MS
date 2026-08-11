using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace IAMS.Infrastructure.Emails;

/// <summary>
/// Delivers queued emails. Uses SMTP when <see cref="SmtpOptions.Host"/> is configured,
/// otherwise falls back to logging (development mode).
/// </summary>
public interface IEmailDispatcher
{
    Task DeliverAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed class EmailDispatcher : IEmailDispatcher
{
    private readonly SmtpOptions _options;
    private readonly ILogger<EmailDispatcher> _logger;

    public EmailDispatcher(IOptions<SmtpOptions> options, ILogger<EmailDispatcher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task DeliverAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogInformation("Email (logged, SMTP not configured) -> To={To} | Subject={Subject} | Body={Body}",
                message.To, message.Subject, message.Body);
            return;
        }

        using var client = new SmtpClient
        {
            Host = _options.Host,
            Port = _options.Port,
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);

        var mailMessage = new MailMessage(_options.From, message.To, message.Subject, message.Body);
        if (!string.IsNullOrWhiteSpace(message.Bcc))
            mailMessage.Bcc.Add(message.Bcc);

        await client.SendMailAsync(mailMessage, cancellationToken);
    }
}