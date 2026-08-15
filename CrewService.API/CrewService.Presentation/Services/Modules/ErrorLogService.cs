using CrewService.Domain.Constants;
using CrewService.Domain.Diagnostics;
using CrewService.Domain.Models.UserAccess;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class ErrorLogService(IErrorLogQuery errorLogQuery, IErrorLogCommand errorLogCommand)
    : ErrorLogSrvc.ErrorLogSrvcBase
{
    public override async Task<GetAllErrorLogsResponse> GetAllErrorLogsAsync(
        GetAllErrorLogsRequest request,
        ServerCallContext context)
    {
        var railroadCtrlNbr = TryGetSelectedRailroadCtrlNbr(context);
        if (!railroadCtrlNbr.HasValue)
            return new GetAllErrorLogsResponse();

        var pageSize = request.PageSize > 0 ? request.PageSize : 200;
        var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;

        var parentCtrlNbr = ResolveParentCtrlNbr(context);

        DateTime? dateFromUtc = DateTime.TryParse(request.DateFrom, out var fromUtc) ? fromUtc : null;
        DateTime? dateToUtc = DateTime.TryParse(request.DateTo, out var toUtc) ? toUtc : null;

        var filter = new ErrorLogFilter(
            SearchText: string.IsNullOrWhiteSpace(request.SearchText) ? null : request.SearchText,
            DateFromUtc: dateFromUtc,
            DateToUtc: dateToUtc,
            Severity: string.IsNullOrWhiteSpace(request.Severity) ? null : request.Severity,
            SourceApp: string.IsNullOrWhiteSpace(request.SourceApp) ? null : request.SourceApp,
            ErrorKind: string.IsNullOrWhiteSpace(request.ErrorKind) ? null : request.ErrorKind,
            Status: string.IsNullOrWhiteSpace(request.Status) ? null : request.Status,
            FingerprintHash: string.IsNullOrWhiteSpace(request.FingerprintHash) ? null : request.FingerprintHash,
            ParentCtrlNbr: parentCtrlNbr,
            RailroadCtrlNbr: railroadCtrlNbr);

        var (entries, totalCount) = await errorLogQuery.GetPagedAsync(
            pageNumber,
            pageSize,
            filter,
            context.CancellationToken);

        var response = new GetAllErrorLogsResponse
        {
            TotalCount = totalCount
        };

        foreach (var entry in entries)
        {
            response.Entries.Add(new ErrorLogEntry
            {
                ErrorId = entry.ErrorId.ToString(),
                OccurredAtUtc = entry.OccurredAtUtc.ToString("O"),
                Severity = entry.Severity,
                SourceApp = entry.SourceApp,
                SourceLayer = entry.SourceLayer,
                ErrorCode = entry.ErrorCode,
                ExceptionType = entry.ExceptionType,
                Message = entry.Message,
                TraceId = entry.TraceId,
                Route = entry.Route ?? string.Empty,
                Method = entry.Method ?? string.Empty,
                PerformedBy = entry.PerformedBy,
                PayloadJson = entry.PayloadJson ?? string.Empty,
                LoggedAtUtc = entry.LoggedAtUtc.ToString("O"),
                ErrorKind = entry.ErrorKind,
                Status = entry.Status,
                FingerprintHash = entry.FingerprintHash,
                OccurrenceCount = entry.OccurrenceCount,
                FirstOccurredAtUtc = entry.FirstOccurredAtUtc.ToString("O"),
                LastOccurredAtUtc = entry.LastOccurredAtUtc.ToString("O"),
                ResolvedAtUtc = entry.ResolvedAtUtc?.ToString("O") ?? string.Empty,
                ResolvedBy = entry.ResolvedBy ?? string.Empty,
                SuppressionReason = entry.SuppressionReason ?? string.Empty
            });
        }

        return response;
    }

    public override async Task<UpdateErrorLogStatusResponse> UpdateErrorLogStatusAsync(
        UpdateErrorLogStatusRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ErrorId, out var errorId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ErrorId must be a valid GUID."));

        if (string.IsNullOrWhiteSpace(request.Status))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Status is required."));

        var updated = await errorLogCommand.UpdateStatusAsync(
            errorId,
            request.Status,
            context.GetHttpContext().User.Identity?.Name ?? string.Empty,
            string.IsNullOrWhiteSpace(request.SuppressionReason) ? null : request.SuppressionReason,
            context.CancellationToken);

        return new UpdateErrorLogStatusResponse { Updated = updated };
    }

    private static long? ResolveParentCtrlNbr(ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();

        var header = httpContext.Request.Headers["x-parent-ctrl-nbr"].FirstOrDefault();
        if (long.TryParse(header, out var fromHeader) && fromHeader > 0)
            return fromHeader;

        var user = httpContext.User;
        if (user.IsInRole(Roles.SystemAdmin))
            return null;

        var firstParent = user.Claims
            .Where(c => c.Type == CustomClaimTypes.ParentRole)
            .Select(c => c.Value.Split(':'))
            .Where(parts => parts.Length >= 2 && long.TryParse(parts[0], out _))
            .Select(parts => long.Parse(parts[0]))
            .FirstOrDefault();

        return firstParent > 0 ? firstParent : null;
    }

    private static long? TryGetSelectedRailroadCtrlNbr(ServerCallContext context)
    {
        var header = context.GetHttpContext().Request.Headers["x-railroad-ctrl-nbr"].FirstOrDefault();
        return long.TryParse(header, out var railroadCtrlNbr) && railroadCtrlNbr > 0
            ? railroadCtrlNbr
            : null;
    }
}
