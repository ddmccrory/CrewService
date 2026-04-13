using CrewService.BlazorUI.Models.Account;
using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

internal sealed class AuthClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<AccountClient> logger) 
: BaseGrpcClient<AuthSrvc.AuthSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new AuthSrvc.AuthSrvcClient(callInvoker), logger, addAuthHeader: false)
{
    #region Methods

    public async Task<AcceptInvitationResponse> AcceptInvitationAsync(AcceptInvitationInputModel model)
    {
        try
        { 
        AcceptInvitationRequest request = new()
        {
            InvitationToken = model.InvitationToken,
            Password = model.Password
        };

        return await _client.AcceptInvitationAsync(request);
        }
        catch (Exception ex)
        {
            base.LogException(ex);
            throw;
        }
    }

    public async Task<AuthResponse> AuthenticateUserAsync(LoginInputModel loginModel)
    {
        try
        { 
        AuthRequest request = new()
        {
            UserName = loginModel.Email,
            Password = loginModel.Password
        };

        return await _client.AuthenticateUserAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex); // Use base class logging
            throw;
        }
    }

    #endregion
}
