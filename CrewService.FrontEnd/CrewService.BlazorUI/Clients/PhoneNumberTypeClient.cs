using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

internal sealed class PhoneNumberTypeClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<PhoneNumberTypeClient> logger)
    : BaseGrpcClient<PhoneNumberTypeSrvc.PhoneNumberTypeSrvcClient>(channelProvider, tokenProvider, callInvoker => new PhoneNumberTypeSrvc.PhoneNumberTypeSrvcClient(callInvoker), logger)
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
