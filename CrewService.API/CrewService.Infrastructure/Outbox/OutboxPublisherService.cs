using CrewService.Domain.Interfaces;
using CrewService.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrewService.Infrastructure.Outbox;

/// <summary>
/// Hybrid background service that:
/// 1. Immediately publishes messages dispatched in-process via Channel
/// 2. Polls for missed/failed messages as fallback
/// </summary>
public sealed class OutboxPublisherService(
    IServiceScopeFactory scopeFactory,
    IMessagePublisher messagePublisher,
    OutboxDispatcher dispatcher,
    IOptions<OutboxPublisherOptions> options,
    ILogger<OutboxPublisherService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IMessagePublisher _messagePublisher = messagePublisher;
    private readonly OutboxDispatcher _dispatcher = dispatcher;
    private readonly ILogger<OutboxPublisherService> _logger = logger;
    private readonly OutboxPublisherOptions _options = options.Value;

    private DateTime _lastCleanupTime = DateTime.MinValue;
    private DateTime _lastPollTime = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Outbox publisher is disabled.");
            return;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Outbox publisher started (hybrid mode). PollingInterval: {PollingInterval}, BatchSize: {BatchSize}",
                _options.PollingInterval, _options.BatchSize);
        }

        // Run both tasks concurrently
        var channelTask = ProcessChannelMessagesAsync(stoppingToken);
        var pollingTask = ProcessPollingAsync(stoppingToken);

        await Task.WhenAll(channelTask, pollingTask);

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Outbox publisher stopped.");
    }

    /// <summary>
    /// Immediately processes messages dispatched in-process via Channel.
    /// </summary>
    private async Task ProcessChannelMessagesAsync(CancellationToken stoppingToken)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Channel message processor started.");

        await foreach (var message in _dispatcher.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await PublishAndUpdateAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex,
                        "Error publishing channel message. MessageId: {MessageId}, EventType: {EventType}",
                        message.MessageId, message.EventType);
                }
            }
        }
    }

    /// <summary>
    /// Fallback polling for missed/failed messages and cleanup.
    /// </summary>
    private async Task ProcessPollingAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (ShouldPoll())
                {
                    await ProcessPendingMessagesAsync(stoppingToken);
                    _lastPollTime = DateTime.UtcNow;
                }

                if (_options.EnableCleanup && ShouldRunCleanup())
                {
                    await CleanupOldMessagesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                    _logger.LogError(ex, "Error in outbox polling cycle.");
            }

            await Task.Delay(_options.PollingInterval, stoppingToken);
        }
    }

    private bool ShouldPoll()
    {
        return DateTime.UtcNow - _lastPollTime >= _options.PollingInterval;
    }

    private async Task PublishAndUpdateAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var published = await _messagePublisher.PublishAsync(
            eventType: message.EventType,
            aggregateType: message.AggregateType,
            aggregateId: message.AggregateId,
            payloadJson: message.PayloadJson,
            correlationId: message.CorrelationId,
            messageId: message.MessageId,
            cancellationToken: cancellationToken);

        // Update the message status in the database
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IOutboxDbContext>();

        var dbMessage = await dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.MessageId == message.MessageId, cancellationToken);

        if (dbMessage is null)
            return;

        if (published)
        {
            dbMessage.MarkAsPublished();
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Published message (channel). MessageId: {MessageId}, EventType: {EventType}",
                    message.MessageId, message.EventType);
            }
        }
        else
        {
            dbMessage.MarkAsFailed();
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Failed to publish message (channel). MessageId: {MessageId}, Retries: {Retries}",
                    message.MessageId, dbMessage.Retries);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IOutboxDbContext>();

        var pendingMessages = await dbContext.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending && m.Retries < _options.MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
            return;

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Polling: Processing {Count} pending outbox messages.", pendingMessages.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var message in pendingMessages)
        {
            try
            {
                var published = await _messagePublisher.PublishAsync(
                    eventType: message.EventType,
                    aggregateType: message.AggregateType,
                    aggregateId: message.AggregateId,
                    payloadJson: message.PayloadJson,
                    correlationId: message.CorrelationId,
                    messageId: message.MessageId,
                    cancellationToken: cancellationToken);

                if (published)
                {
                    message.MarkAsPublished();
                    successCount++;
                }
                else
                {
                    message.MarkAsFailed();
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                message.MarkAsFailed();
                failCount++;
                if (_logger.IsEnabled(LogLevel.Error))
                    _logger.LogError(ex, "Exception publishing outbox message. MessageId: {MessageId}", message.MessageId);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if ((successCount > 0 || failCount > 0) && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Polling batch completed. Published: {SuccessCount}, Failed: {FailCount}",
                successCount, failCount);
        }
    }

    private bool ShouldRunCleanup()
    {
        return DateTime.UtcNow - _lastCleanupTime > _options.CleanupInterval;
    }

    private async Task CleanupOldMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IOutboxDbContext>();

        var cutoffDate = DateTime.UtcNow - _options.RetentionPeriod;

        var deletedCount = await dbContext.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Published && m.PublishedAt < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);

        _lastCleanupTime = DateTime.UtcNow;

        if (deletedCount > 0 && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Outbox cleanup: Deleted {Count} old messages.", deletedCount);
        }
    }
}