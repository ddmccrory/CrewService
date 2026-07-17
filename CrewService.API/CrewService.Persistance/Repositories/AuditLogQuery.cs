using CrewService.Domain.DomainEvents;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class AuditLogQuery(CrewServiceDbContext dbContext) : IAuditLogQuery
{
    public async Task<(IReadOnlyList<DomainEventLog> Entries, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, AuditLogFilter? filter = null, CancellationToken ct = default)
    {
        var query = dbContext.DomainEventLogs
            .AsNoTracking()
            .AsQueryable();

        if (filter is not null)
        {
            if (filter.ParentCtrlNbr.HasValue)
                query = query.Where(e => e.ParentCtrlNbr == filter.ParentCtrlNbr.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var pattern = $"%{filter.SearchText.ToLowerInvariant()}%";
                query = query.Where(e =>
                    EF.Functions.Like(e.EventType.ToLower(), pattern) ||
                    EF.Functions.Like(e.AggregateType.ToLower(), pattern) ||
                    EF.Functions.Like(e.PerformedBy.ToLower(), pattern) ||
                    (e.PayloadJson != null && EF.Functions.Like(e.PayloadJson.ToLower(), pattern)));
            }

            if (filter.DateFrom.HasValue)
                query = query.Where(e => e.OccurredAt >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(e => e.OccurredAt <= filter.DateTo.Value);
        }

        var orderedQuery = query.OrderByDescending(e => e.OccurredAt);

        var totalCount = await orderedQuery.CountAsync(ct);

        var entries = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (entries, totalCount);
    }
}
