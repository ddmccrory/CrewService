using CrewService.Domain.Constants;
using CrewService.Domain.DomainEvents;
using CrewService.Domain.Models.UserAccess;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class AuditLogService(IAuditLogQuery auditLogQuery)
    : AuditLogSrvc.AuditLogSrvcBase
{
    public override async Task<GetAllAuditLogsResponse> GetAllAuditLogsAsync(
        GetAllAuditLogsRequest request, ServerCallContext context)
    {
        var pageSize = request.PageSize > 0 ? request.PageSize : 200;
        var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;

        var parentCtrlNbr = ResolveParentCtrlNbr(context);

        AuditLogFilter? filter = null;
        if (parentCtrlNbr.HasValue ||
            !string.IsNullOrWhiteSpace(request.SearchText) ||
            !string.IsNullOrWhiteSpace(request.DateFrom) ||
            !string.IsNullOrWhiteSpace(request.DateTo))
        {
            DateTime? dateFrom = DateTime.TryParse(request.DateFrom, out var df) ? df : null;
            DateTime? dateTo = DateTime.TryParse(request.DateTo, out var dt) ? dt : null;

            filter = new AuditLogFilter(
                SearchText: string.IsNullOrWhiteSpace(request.SearchText) ? null : request.SearchText,
                DateFrom: dateFrom,
                DateTo: dateTo,
                ParentCtrlNbr: parentCtrlNbr);
        }

        var (entries, totalCount) = await auditLogQuery.GetPagedAsync(
            pageNumber, pageSize, filter, context.CancellationToken);

        var response = new GetAllAuditLogsResponse { TotalCount = totalCount };

        foreach (var entry in entries)
        {
            response.Entries.Add(new AuditLogEntry
            {
                EventId = entry.EventId.ToString(),
                EventType = entry.EventType,
                AggregateType = entry.AggregateType,
                AggregateId = entry.AggregateId,
                OccurredAt = entry.OccurredAt.ToString("O"),
                PayloadJson = entry.PayloadJson ?? string.Empty,
                PerformedBy = entry.PerformedBy,
                LoggedAtUtc = entry.LoggedAtUtc.ToString("O")
            });
        }

        return response;
    }

    /// <summary>
    /// Returns the parent CtrlNbr to filter by. Non-SystemAdmin users are always
    /// scoped to their <c>x-parent-ctrl-nbr</c> header; SystemAdmin sees all
    /// unless a specific parent is selected.
    /// </summary>
    private static long? ResolveParentCtrlNbr(ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();

        var header = httpContext.Request.Headers["x-parent-ctrl-nbr"].FirstOrDefault();
        if (long.TryParse(header, out var fromHeader) && fromHeader > 0)
            return fromHeader;

        var user = httpContext.User;
        if (user.IsInRole(Roles.SystemAdmin))
            return null;

        // Non-SystemAdmin without a header: fall back to first parent from claims
        var firstParent = user.Claims
            .Where(c => c.Type == CustomClaimTypes.ParentRole)
            .Select(c => c.Value.Split(':'))
            .Where(parts => parts.Length >= 2 && long.TryParse(parts[0], out _))
            .Select(parts => long.Parse(parts[0]))
            .FirstOrDefault();

        return firstParent > 0 ? firstParent : null;
    }
}
