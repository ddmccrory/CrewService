using CrewService.Domain.Diagnostics;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class ErrorLogRepository(CrewServiceDbContext dbContext)
    : IErrorLogQuery, IErrorLogCommand
{
    public async Task<(IReadOnlyList<ErrorLog> Entries, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        ErrorLogFilter? filter = null,
        CancellationToken ct = default)
    {
        var query = dbContext.ErrorLogs
            .AsNoTracking()
            .AsQueryable();

        if (filter is not null)
        {
            if (filter.ParentCtrlNbr.HasValue)
                query = query.Where(e => e.ParentCtrlNbr == filter.ParentCtrlNbr.Value);

            if (filter.RailroadCtrlNbr.HasValue)
                query = query.Where(e => e.RailroadCtrlNbr == filter.RailroadCtrlNbr.Value || e.RailroadCtrlNbr == null);

            if (!string.IsNullOrWhiteSpace(filter.Severity))
                query = query.Where(e => e.Severity == filter.Severity);

            if (!string.IsNullOrWhiteSpace(filter.SourceApp))
                query = query.Where(e => e.SourceApp == filter.SourceApp);

            if (!string.IsNullOrWhiteSpace(filter.ErrorKind))
                query = query.Where(e => e.ErrorKind == filter.ErrorKind);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(e => e.Status == filter.Status);

            if (!string.IsNullOrWhiteSpace(filter.FingerprintHash))
                query = query.Where(e => e.FingerprintHash == filter.FingerprintHash);

            if (filter.DateFromUtc.HasValue)
                query = query.Where(e => e.OccurredAtUtc >= filter.DateFromUtc.Value);

            if (filter.DateToUtc.HasValue)
                query = query.Where(e => e.OccurredAtUtc <= filter.DateToUtc.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var pattern = $"%{filter.SearchText.ToLowerInvariant()}%";
                query = query.Where(e =>
                    EF.Functions.Like(e.ErrorCode.ToLower(), pattern) ||
                    EF.Functions.Like(e.ExceptionType.ToLower(), pattern) ||
                    EF.Functions.Like(e.Message.ToLower(), pattern) ||
                    EF.Functions.Like(e.TraceId.ToLower(), pattern) ||
                    EF.Functions.Like(e.FingerprintHash.ToLower(), pattern) ||
                    EF.Functions.Like(e.SourceApp.ToLower(), pattern) ||
                    EF.Functions.Like(e.SourceLayer.ToLower(), pattern) ||
                    EF.Functions.Like(e.ErrorKind.ToLower(), pattern) ||
                    EF.Functions.Like(e.Status.ToLower(), pattern) ||
                    EF.Functions.Like(e.PerformedBy.ToLower(), pattern) ||
                    (e.Route != null && EF.Functions.Like(e.Route.ToLower(), pattern)) ||
                    (e.Method != null && EF.Functions.Like(e.Method.ToLower(), pattern)) ||
                    (e.PayloadJson != null && EF.Functions.Like(e.PayloadJson.ToLower(), pattern)));
            }
        }

        var orderedQuery = query
            .OrderByDescending(e => e.OccurrenceCount)
            .ThenByDescending(e => e.LastOccurredAtUtc)
            .ThenByDescending(e => e.LoggedAtUtc);

        var totalCount = await orderedQuery.CountAsync(ct);

        var entries = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (entries, totalCount);
    }

    public async Task<bool> UpdateStatusAsync(
        Guid errorId,
        string status,
        string actedBy,
        string? suppressionReason = null,
        CancellationToken ct = default)
    {
        var entry = await dbContext.ErrorLogs
            .FirstOrDefaultAsync(e => e.ErrorId == errorId, ct);

        if (entry is null)
            return false;

        entry.SetStatus(status, actedBy, suppressionReason);
        await dbContext.SaveChangesAsync(ct);
        return true;
    }
}
