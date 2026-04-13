using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class AuditLogClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<AuditLogClient> logger)
    : BaseGrpcClient<AuditLogSrvc.AuditLogSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new AuditLogSrvc.AuditLogSrvcClient(callInvoker), logger)
{
    public async Task<GetAllAuditLogsResponse> GetAllAsync(
        int pageNumber = 1, int pageSize = 200,
        string? searchText = null, string? dateFrom = null, string? dateTo = null)
    {
        try
        {
            var request = new GetAllAuditLogsRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            if (!string.IsNullOrWhiteSpace(searchText))
                request.SearchText = searchText;
            if (!string.IsNullOrWhiteSpace(dateFrom))
                request.DateFrom = dateFrom;
            if (!string.IsNullOrWhiteSpace(dateTo))
                request.DateTo = dateTo;

            return await _client.GetAllAuditLogsAsyncAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
