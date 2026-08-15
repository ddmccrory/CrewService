using CrewService.Domain.Diagnostics;
using CrewService.Domain.Interfaces;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrewService.Persistance.Repositories;

internal sealed class ErrorLogAlertingService(
    IServiceScopeFactory scopeFactory,
    IOptions<ErrorLogOperationalReadinessOptions> options,
    ILogger<ErrorLogAlertingService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ErrorLogOperationalReadinessOptions _options = options.Value;
    private readonly ILogger<ErrorLogAlertingService> _logger = logger;
    private DateTime _lastObservedUtc = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableAlerting)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Error log alerting is disabled.");
            return;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Error log alerting started. AlertWindow: {AlertWindow}, Threshold: {Threshold}, SeverityMinimum: {SeverityMinimum}, Channel: {Channel}",
                _options.AlertWindow,
                _options.AlertOccurrenceThreshold,
                _options.AlertSeverityMinimum,
                _options.AlertChannel);
        }

        await EvaluateForTestingAsync(stoppingToken);

        using var timer = new PeriodicTimer(_options.AlertWindow);
        while (!stoppingToken.IsCancellationRequested
               && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await EvaluateForTestingAsync(stoppingToken);
        }
    }

    internal async Task EvaluateForTestingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CrewServiceDbContext>();
            var operationalNotifier = scope.ServiceProvider.GetRequiredService<IOperationalNotifier>();

            var nowUtc = DateTime.UtcNow;
            var windowStartUtc = nowUtc - _options.AlertWindow;
            var effectiveStartUtc = _lastObservedUtc > windowStartUtc ? _lastObservedUtc : windowStartUtc;
            var minimumSeverityRank = ResolveSeverityRank(_options.AlertSeverityMinimum);

            var candidates = await dbContext.ErrorLogs
                .AsNoTracking()
                .Where(e =>
                    e.Status != ErrorLogStatuses.Resolved
                    && e.Status != ErrorLogStatuses.Suppressed
                    && e.LastOccurredAtUtc >= effectiveStartUtc)
                .Select(e => new
                {
                    e.ErrorCode,
                    e.ErrorKind,
                    e.SourceApp,
                    e.SourceLayer,
                    e.Message,
                    e.Severity,
                    e.OccurrenceCount,
                    e.LastOccurredAtUtc,
                    e.FingerprintHash
                })
                .ToListAsync(cancellationToken);

            var triggered = candidates
                .Where(c => ResolveSeverityRank(c.Severity) >= minimumSeverityRank)
                .Where(c => c.OccurrenceCount >= _options.AlertOccurrenceThreshold)
                .OrderByDescending(c => c.OccurrenceCount)
                .ThenByDescending(c => c.LastOccurredAtUtc)
                .Take(10)
                .ToList();

            if (triggered.Count == 0)
            {
                _lastObservedUtc = nowUtc;
                return;
            }

            var channel = ResolveChannel(_options.AlertChannel);
            var subject = $"Error Log Alert: {triggered.Count} high-occurrence active entries";
            var body = string.Join(Environment.NewLine, triggered.Select(t =>
                $"[{t.Severity}] {t.ErrorCode} ({t.ErrorKind}) Count={t.OccurrenceCount} Source={t.SourceApp}/{t.SourceLayer} Last={t.LastOccurredAtUtc:O} Fingerprint={t.FingerprintHash} Message={t.Message}"));

            await operationalNotifier.SendAsync(channel, subject, body, cancellationToken);
            _lastObservedUtc = nowUtc;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Ignore cancellation during shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error log alerting evaluation failed.");
        }
    }

    private static NotificationChannel ResolveChannel(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured)
            && Enum.TryParse<NotificationChannel>(configured, true, out var parsed))
            return parsed;

        return NotificationChannel.SystemSupport;
    }

    private static int ResolveSeverityRank(string? severity)
    {
        return severity switch
        {
            "Critical" => 4,
            "Error" => 3,
            "Warning" => 2,
            _ => 1
        };
    }
}
