using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

/// <summary>
/// Unauthenticated gRPC client for invitation token validation.
/// Used by the AcceptInvitation page before the user has logged in.
/// </summary>
internal sealed class InvitationTokenClient(GrpcChannelProvider channelProvider, IHttpContextAccessor httpContextAccessor, ILogger<InvitationTokenClient> logger)
    : BaseGrpcClient<InvitationSrvc.InvitationSrvcClient>(channelProvider, httpContextAccessor, callInvoker => new InvitationSrvc.InvitationSrvcClient(callInvoker), logger, addAuthHeader: false)
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
