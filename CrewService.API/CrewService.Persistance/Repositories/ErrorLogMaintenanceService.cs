using CrewService.Domain.Diagnostics;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrewService.Persistance.Repositories;

internal sealed class ErrorLogMaintenanceService(
    IServiceScopeFactory scopeFactory,
    IOptions<ErrorLogOperationalReadinessOptions> options,
    ILogger<ErrorLogMaintenanceService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ErrorLogOperationalReadinessOptions _options = options.Value;
    private readonly ILogger<ErrorLogMaintenanceService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableRetentionCleanup)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Error log retention cleanup is disabled.");
            return;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Error log retention cleanup started. RetentionPeriod: {RetentionPeriod}, CleanupInterval: {CleanupInterval}",
                _options.RetentionPeriod,
                _options.CleanupInterval);
        }

        await RunCleanupForTestingAsync(stoppingToken);

        using var timer = new PeriodicTimer(_options.CleanupInterval);
        while (!stoppingToken.IsCancellationRequested
               && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCleanupForTestingAsync(stoppingToken);
        }
    }

    internal async Task RunCleanupForTestingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CrewServiceDbContext>();

            var cutoffUtc = DateTime.UtcNow - _options.RetentionPeriod;

            var deletedCount = await dbContext.ErrorLogs
                .Where(e =>
                    e.Status == ErrorLogStatuses.Resolved
                    && e.ResolvedAtUtc != null
                    && e.ResolvedAtUtc < cutoffUtc)
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount > 0 && _logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Error log retention cleanup deleted {DeletedCount} resolved entries older than {CutoffUtc:O}.",
                    deletedCount,
                    cutoffUtc);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // ignore shutdown cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error log retention cleanup failed.");
        }
    }
}
