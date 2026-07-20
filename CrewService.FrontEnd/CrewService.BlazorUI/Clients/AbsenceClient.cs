using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class AbsenceClient(
    GrpcChannelProvider channelProvider,
    CircuitTokenProvider tokenProvider,
    AppContextService appContext,
    ILogger<AbsenceClient> logger)
    : BaseGrpcClient<MarkOffSrvc.MarkOffSrvcClient>(
        channelProvider,
        tokenProvider,
        appContext,
        callInvoker => new MarkOffSrvc.MarkOffSrvcClient(callInvoker),
        logger)
{
    public async Task<GetMarkOffCodesResponse> GetAbsenceCodesAsync(bool activeOnly)
    {
        try
        {
            return await _client.GetMarkOffCodesAsync(new GetMarkOffCodesMsg
            {
                ActiveOnly = activeOnly
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<MarkOffCodeResponse> CreateAbsenceCodeAsync(
        string code,
        string description,
        bool isExcused,
        bool isCompensated,
        bool requiresApproval,
        bool isSystemOnly,
        bool isHolidayExempt,
        decimal? defaultAutoMarkUpHours,
        bool isActive)
    {
        try
        {
            var request = new CreateMarkOffCodeMsg
            {
                Code = code,
                Description = description,
                IsExcused = isExcused,
                IsCompensated = isCompensated,
                RequiresApproval = requiresApproval,
                IsSystemOnly = isSystemOnly,
                IsHolidayExempt = isHolidayExempt,
                IsActive = isActive
            };

            if (defaultAutoMarkUpHours.HasValue)
                request.DefaultAutoMarkUpHours = Convert.ToDouble(defaultAutoMarkUpHours.Value);

            return await _client.CreateMarkOffCodeAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<MarkOffCodeResponse> UpdateAbsenceCodeAsync(
        long ctrlNbr,
        string code,
        string description,
        bool isExcused,
        bool isCompensated,
        bool requiresApproval,
        bool isSystemOnly,
        bool isHolidayExempt,
        decimal? defaultAutoMarkUpHours,
        bool isActive)
    {
        try
        {
            var request = new UpdateMarkOffCodeMsg
            {
                CtrlNbr = ctrlNbr,
                Code = code,
                Description = description,
                IsExcused = isExcused,
                IsCompensated = isCompensated,
                RequiresApproval = requiresApproval,
                IsSystemOnly = isSystemOnly,
                IsHolidayExempt = isHolidayExempt,
                IsActive = isActive
            };

            if (defaultAutoMarkUpHours.HasValue)
                request.DefaultAutoMarkUpHours = Convert.ToDouble(defaultAutoMarkUpHours.Value);

            return await _client.UpdateMarkOffCodeAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DeleteResponse> DeleteAbsenceCodeAsync(long ctrlNbr)
    {
        try
        {
            return await _client.DeleteMarkOffCodeAsync(new DeleteMarkOffCodeMsg
            {
                CtrlNbr = ctrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
