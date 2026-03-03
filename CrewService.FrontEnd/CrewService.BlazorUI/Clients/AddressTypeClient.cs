using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

internal sealed class AddressTypeClient(GrpcChannelProvider channelProvider, IHttpContextAccessor httpContextAccessor, ILogger<AddressTypeClient> logger)
    : BaseGrpcClient<AddressTypeSrvc.AddressTypeSrvcClient>(channelProvider, httpContextAccessor, callInvoker => new AddressTypeSrvc.AddressTypeSrvcClient(callInvoker), logger)
{
    public async Task<GetAllAddressTypeResponse> GetAllAsync(long clientCtrlNbr)
    {
        try
        {
            return await _client.GetAllAsyncAsync(new GetAllAddressTypeRequest { ClientCtrlNbr = clientCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
