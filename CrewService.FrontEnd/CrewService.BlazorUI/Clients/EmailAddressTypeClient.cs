using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

internal sealed class EmailAddressTypeClient(GrpcChannelProvider channelProvider, IHttpContextAccessor httpContextAccessor, ILogger<EmailAddressTypeClient> logger)
    : BaseGrpcClient<EmailAddressTypeSrvc.EmailAddressTypeSrvcClient>(channelProvider, httpContextAccessor, callInvoker => new EmailAddressTypeSrvc.EmailAddressTypeSrvcClient(callInvoker), logger)
{
    public async Task<GetAllEmailAddressTypeResponse> GetAllAsync(long clientCtrlNbr)
    {
        try
        {
            return await _client.GetAllAsyncAsync(new GetAllEmailAddressTypeRequest { ClientCtrlNbr = clientCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
