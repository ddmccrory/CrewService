using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

internal sealed class PhoneNumberTypeClient(GrpcChannelProvider channelProvider, IHttpContextAccessor httpContextAccessor, ILogger<PhoneNumberTypeClient> logger)
    : BaseGrpcClient<PhoneNumberTypeSrvc.PhoneNumberTypeSrvcClient>(channelProvider, httpContextAccessor, callInvoker => new PhoneNumberTypeSrvc.PhoneNumberTypeSrvcClient(callInvoker), logger)
{
    public async Task<GetAllPhoneNumberTypeResponse> GetAllAsync(long clientCtrlNbr)
    {
        try
        {
            return await _client.GetAllAsyncAsync(new GetAllPhoneNumberTypeRequest { ClientCtrlNbr = clientCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
