using System.Threading.Channels;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Outbox;

namespace CrewService.Infrastructure.Outbox;

/// <summary>
/// In-memory dispatcher that queues outbox messages for immediate publishing.
/// </summary>
public sealed class OutboxDispatcher : IOutboxDispatcher
{
    private readonly Channel<OutboxMessage> _channel;

    public OutboxDispatcher()
    {
        _channel = Channel.CreateUnbounded<OutboxMessage>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public ChannelReader<OutboxMessage> Reader => _channel.Reader;

    public void EnqueueForDispatch(IReadOnlyList<OutboxMessage> messages)
    {
        foreach (var message in messages)
        {
            _channel.Writer.TryWrite(message);
        }
    }
}