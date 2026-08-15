using CrewService.Domain.Diagnostics;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CrewService.Persistance.Repositories;

internal sealed class ErrorLogWriter(IServiceScopeFactory scopeFactory) : IErrorLogWriter
{
    private static readonly Regex s_jsonSecretRegex = new(
        "\"(?<key>password|passphrase|secret|token|apiKey|authorization|accessToken|refreshToken)\"\\s*:\\s*\"[^\"]*\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex s_bearerRegex = new(
        "Bearer\\s+[A-Za-z0-9\\-._~+/]+=*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task WriteAsync(ErrorLogWriteRequest request, CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrewServiceDbContext>();

        var normalizedOccurredAtUtc = EnsureUtc(request.OccurredAtUtc);
        var sanitizedMessage = RedactSensitiveText(TruncateOrDefault(request.Message, 4000, "An error occurred."))
            ?? "An error occurred.";
        var sanitizedPayload = RedactSensitiveText(request.PayloadJson);
        var normalizedSeverity = TruncateOrDefault(request.Severity, 32, "Error");
        var normalizedErrorKind = TruncateOrDefault(request.ErrorKind, 64, ErrorLogKinds.UnhandledException);
        var normalizedTraceId = TruncateOrDefault(request.TraceId, 128, string.Empty);
        var normalizedStatus = TruncateOrDefault(request.Status, 32, ErrorLogStatuses.New);

        var fingerprintHash = ResolveFingerprintHash(request, normalizedErrorKind, sanitizedMessage);

        var existing = await dbContext.ErrorLogs
            .FirstOrDefaultAsync(e =>
                e.FingerprintHash == fingerprintHash
                && e.SourceApp == TruncateOrDefault(request.SourceApp, 64, "Unknown")
                && e.SourceLayer == TruncateOrDefault(request.SourceLayer, 64, "Unhandled")
                && e.Status != ErrorLogStatuses.Resolved
                && e.Status != ErrorLogStatuses.Suppressed,
                ct);

        if (existing is not null)
        {
            existing.RegisterOccurrence(
                normalizedOccurredAtUtc,
                normalizedSeverity,
                normalizedTraceId,
                sanitizedMessage,
                sanitizedPayload);

            if (!string.Equals(normalizedStatus, ErrorLogStatuses.New, StringComparison.Ordinal))
                existing.SetStatus(normalizedStatus, request.PerformedBy);

            await dbContext.SaveChangesAsync(ct);
            return;
        }

        var entry = ErrorLog.Create(
            occurredAtUtc: normalizedOccurredAtUtc,
            errorKind: normalizedErrorKind,
            sourceApp: TruncateOrDefault(request.SourceApp, 64, "Unknown"),
            sourceLayer: TruncateOrDefault(request.SourceLayer, 64, "Unhandled"),
            severity: normalizedSeverity,
            fingerprintHash: fingerprintHash,
            errorCode: TruncateOrDefault(request.ErrorCode, 100, "UNHANDLED_ERROR"),
            exceptionType: TruncateOrDefault(request.ExceptionType, 512, "Exception"),
            message: sanitizedMessage,
            traceId: normalizedTraceId,
            route: Truncate(request.Route, 512),
            method: Truncate(request.Method, 256),
            performedBy: TruncateOrDefault(request.PerformedBy, 100, string.Empty),
            parentCtrlNbr: request.ParentCtrlNbr,
            railroadCtrlNbr: request.RailroadCtrlNbr,
            payloadJson: sanitizedPayload);

        if (!string.Equals(normalizedStatus, ErrorLogStatuses.New, StringComparison.Ordinal))
            entry.SetStatus(request.Status, request.PerformedBy);

        dbContext.ErrorLogs.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }

    private static string ResolveFingerprintHash(ErrorLogWriteRequest request, string normalizedErrorKind, string sanitizedMessage)
    {
        var provided = Truncate(request.FingerprintHash, 64);
        if (!string.IsNullOrWhiteSpace(provided))
            return provided;

        var normalizedMessage = NormalizeForFingerprint(sanitizedMessage);
        var canonical = string.Join("|",
            normalizedErrorKind,
            TruncateOrDefault(request.SourceApp, 64, "Unknown"),
            TruncateOrDefault(request.SourceLayer, 64, "Unhandled"),
            TruncateOrDefault(request.ErrorCode, 100, "UNHANDLED_ERROR"),
            TruncateOrDefault(request.ExceptionType, 512, "Exception"),
            Truncate(request.Route, 512) ?? string.Empty,
            Truncate(request.Method, 256) ?? string.Empty,
            normalizedMessage);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes);
    }

    private static string NormalizeForFingerprint(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, "[0-9]+", "#");
        normalized = Regex.Replace(normalized, "\\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\\b", "{guid}");
        normalized = Regex.Replace(normalized, "\\s+", " ");
        return normalized;
    }

    private static string? RedactSensitiveText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var redacted = s_jsonSecretRegex.Replace(input, m =>
        {
            var key = m.Groups["key"].Value;
            return $"\"{key}\":\"***REDACTED***\"";
        });

        redacted = s_bearerRegex.Replace(redacted, "Bearer ***REDACTED***");
        return redacted;
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static string TruncateOrDefault(string? value, int maxLength, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}
