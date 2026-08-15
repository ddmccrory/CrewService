using CrewService.Domain.Diagnostics;
using CrewService.Domain.Interfaces;
using CrewService.Persistance.Data;
using CrewService.Persistance.Encryption;
using CrewService.Persistance.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CrewService.UnitTests.Persistance;

public sealed class ErrorLogOperationalReadinessServiceTests
{
    [Fact]
    public async Task ErrorLogMaintenanceService_DeletesOldResolvedEntries()
    {
        var dbName = $"ErrorLogRetention_{Guid.NewGuid():N}";
        await using var serviceProvider = BuildProvider(dbName);

        await SeedAsync(serviceProvider, DateTime.UtcNow);

        var options = Options.Create(new ErrorLogOperationalReadinessOptions
        {
            EnableRetentionCleanup = true,
            RetentionPeriod = TimeSpan.FromDays(30),
            CleanupInterval = TimeSpan.FromDays(1)
        });

        var sut = new ErrorLogMaintenanceService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<ErrorLogMaintenanceService>.Instance);

        await sut.RunCleanupForTestingAsync(TestContext.Current.CancellationToken);

        await using var verifyScope = serviceProvider.CreateAsyncScope();
        var dbContext = verifyScope.ServiceProvider.GetRequiredService<CrewServiceDbContext>();

        var remaining = await dbContext.ErrorLogs.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(remaining);
        Assert.Equal(ErrorLogStatuses.Resolved, remaining[0].Status);
        Assert.True(remaining[0].ResolvedAtUtc >= DateTime.UtcNow.AddDays(-30));
    }

    [Fact]
    public async Task ErrorLogAlertingService_SendsNotification_ForHighOccurrenceActiveErrors()
    {
        var dbName = $"ErrorLogAlerting_{Guid.NewGuid():N}";
        await using var serviceProvider = BuildProvider(dbName);

        await SeedAlertCandidateAsync(serviceProvider);

        var options = Options.Create(new ErrorLogOperationalReadinessOptions
        {
            EnableAlerting = true,
            AlertWindow = TimeSpan.FromHours(1),
            AlertOccurrenceThreshold = 5,
            AlertSeverityMinimum = "Error",
            AlertChannel = "SystemSupport"
        });

        var notifier = serviceProvider.GetRequiredService<CapturingOperationalNotifier>();

        var sut = new ErrorLogAlertingService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<ErrorLogAlertingService>.Instance);

        await sut.EvaluateForTestingAsync(TestContext.Current.CancellationToken);

        var sent = Assert.Single(notifier.Messages);
        Assert.Equal(CrewService.Domain.Interfaces.NotificationChannel.SystemSupport, sent.Channel);
        Assert.Contains("Error Log Alert", sent.Subject, StringComparison.Ordinal);
        Assert.Contains("ALERT_500", sent.Body, StringComparison.Ordinal);
    }

    private static ServiceProvider BuildProvider(string dbName)
    {
        var services = new ServiceCollection();
        var connection = new SqliteConnection($"Data Source={dbName};Mode=Memory;Cache=Shared");
        connection.Open();

        services.AddSingleton<ICurrentUserService, StubCurrentUserService>();
        services.AddSingleton<IFieldEncryptor, StubFieldEncryptor>();
        services.AddSingleton<CapturingOperationalNotifier>();
        services.AddScoped<IOperationalNotifier>(sp => sp.GetRequiredService<CapturingOperationalNotifier>());
        services.AddSingleton(connection);

        services.AddDbContext<CrewServiceDbContext>(options =>
            options.UseSqlite(connection));

        return services.BuildServiceProvider();
    }

    private sealed class StubFieldEncryptor : IFieldEncryptor
    {
        public string Encrypt(string plainText) => plainText;
        public string Decrypt(string cipherText) => cipherText;
    }

    private static async Task SeedAsync(IServiceProvider serviceProvider, DateTime nowUtc)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrewServiceDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var oldResolved = ErrorLog.Create(
            nowUtc.AddDays(-45),
            ErrorLogKinds.UnhandledException,
            "BackendApi",
            "HTTP",
            "Error",
            "old-resolved-fingerprint",
            "HTTP_500",
            "System.Exception",
            "old resolved",
            "trace-old",
            "/v1/old",
            "GET",
            "tester",
            null,
            null,
            "{}");
        oldResolved.SetStatus(ErrorLogStatuses.Resolved, "tester");
        SetResolvedAt(oldResolved, nowUtc.AddDays(-40));

        var recentResolved = ErrorLog.Create(
            nowUtc.AddDays(-10),
            ErrorLogKinds.UnhandledException,
            "BackendApi",
            "HTTP",
            "Error",
            "recent-resolved-fingerprint",
            "HTTP_500",
            "System.Exception",
            "recent resolved",
            "trace-recent",
            "/v1/recent",
            "GET",
            "tester",
            null,
            null,
            "{}");
        recentResolved.SetStatus(ErrorLogStatuses.Resolved, "tester");
        SetResolvedAt(recentResolved, nowUtc.AddDays(-5));

        dbContext.ErrorLogs.AddRange(oldResolved, recentResolved);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedAlertCandidateAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrewServiceDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var alert = ErrorLog.Create(
            DateTime.UtcNow.AddMinutes(-15),
            ErrorLogKinds.UnhandledException,
            "BackendApi",
            "HTTP",
            "Critical",
            "alert-fingerprint",
            "ALERT_500",
            "System.InvalidOperationException",
            "alert candidate",
            "trace-alert",
            "/v1/alerts",
            "GET",
            "tester",
            null,
            null,
            "{}");

        for (var i = 0; i < 6; i++)
        {
            alert.RegisterOccurrence(
                DateTime.UtcNow.AddMinutes(-10 + i),
                "Critical",
                $"trace-alert-{i}",
                "alert candidate",
                "{}");
        }

        dbContext.ErrorLogs.Add(alert);
        await dbContext.SaveChangesAsync();
    }

    private static void SetResolvedAt(ErrorLog errorLog, DateTime value)
    {
        var property = typeof(ErrorLog).GetProperty(nameof(ErrorLog.ResolvedAtUtc));
        property!.SetValue(errorLog, value);
    }

    private sealed class CapturingOperationalNotifier : IOperationalNotifier
    {
        public List<(NotificationChannel Channel, string Subject, string Body)> Messages { get; } = [];

        public Task SendAsync(
            NotificationChannel channel,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            Messages.Add((channel, subject, body));
            return Task.CompletedTask;
        }
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public Guid GetUserId() => Guid.NewGuid();
        public string GetUserName() => "test-user";
        public string? GetUserIdentifier() => "test-user";
        public bool IsInRole(string roleName) => false;
        public long? GetParentCtrlNbr() => null;
        public void SetAuditOverride(string name) { }
    }
}
