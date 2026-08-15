using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class ErrorLogClient(
    GrpcChannelProvider channelProvider,
    CircuitTokenProvider tokenProvider,
    AppContextService appContext,
    ILogger<ErrorLogClient> logger)
    : BaseGrpcClient<ErrorLogSrvc.ErrorLogSrvcClient>(
        channelProvider,
        tokenProvider,
        appContext,
        callInvoker => new ErrorLogSrvc.ErrorLogSrvcClient(callInvoker),
        logger)
{
    public async Task<GetAllErrorLogsResponse> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 200,
        string? searchText = null,
        string? dateFrom = null,
        string? dateTo = null,
        string? severity = null,
        string? sourceApp = null,
        string? errorKind = null,
        string? status = null,
        string? fingerprintHash = null)
    {
        try
        {
            var request = new GetAllErrorLogsRequest
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
            if (!string.IsNullOrWhiteSpace(severity))
                request.Severity = severity;
            if (!string.IsNullOrWhiteSpace(sourceApp))
                request.SourceApp = sourceApp;
            if (!string.IsNullOrWhiteSpace(errorKind))
                request.ErrorKind = errorKind;
            if (!string.IsNullOrWhiteSpace(status))
                request.Status = status;
            if (!string.IsNullOrWhiteSpace(fingerprintHash))
                request.FingerprintHash = fingerprintHash;

            return await _client.GetAllErrorLogsAsyncAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<bool> UpdateStatusAsync(string errorId, string status, string? suppressionReason = null)
    {
        try
        {
            var request = new UpdateErrorLogStatusRequest
            {
                ErrorId = errorId,
                Status = status
            };

            if (!string.IsNullOrWhiteSpace(suppressionReason))
                request.SuppressionReason = suppressionReason;

            var response = await _client.UpdateErrorLogStatusAsyncAsync(request);
            return response.Updated;
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
