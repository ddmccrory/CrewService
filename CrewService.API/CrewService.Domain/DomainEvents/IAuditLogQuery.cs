namespace CrewService.Domain.DomainEvents;

public sealed record AuditLogFilter(
    string? SearchText = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    long? ParentCtrlNbr = null);

public interface IAuditLogQuery
{
    Task<(IReadOnlyList<DomainEventLog> Entries, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, AuditLogFilter? filter = null, CancellationToken ct = default);
}
