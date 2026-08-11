using System.Threading.Channels;

namespace IAMS.Infrastructure.Emails;

/// <summary>In-memory channel backing the async email pipeline.</summary>
public interface IEmailQueue
{
    void Enqueue(EmailMessage message);
    ChannelReader<EmailMessage> Reader { get; }
}

public sealed class EmailQueue : IEmailQueue
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateUnbounded<EmailMessage>();

    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    public void Enqueue(EmailMessage message)
    {
        if (message is null)
            return;
        _channel.Writer.TryWrite(message);
    }
}