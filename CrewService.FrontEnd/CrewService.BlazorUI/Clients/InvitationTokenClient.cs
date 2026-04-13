using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

/// <summary>
/// Unauthenticated gRPC client for invitation token validation.
/// Used by the AcceptInvitation page before the user has logged in.
/// </summary>
internal sealed class InvitationTokenClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<InvitationTokenClient> logger)
    : BaseGrpcClient<InvitationSrvc.InvitationSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new InvitationSrvc.InvitationSrvcClient(callInvoker), logger, addAuthHeader: false)
{
    public async Task<ValidateInvitationTokenReply> ValidateTokenAsync(string token)
    {
        try
        {
            return await _client.ValidateInvitationTokenAsync(new ValidateInvitationTokenRequest
            {
                Token = token
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
