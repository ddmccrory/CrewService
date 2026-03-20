using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class InvitationsClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<InvitationsClient> logger)
    : BaseGrpcClient<InvitationSrvc.InvitationSrvcClient>(channelProvider, tokenProvider, callInvoker => new InvitationSrvc.InvitationSrvcClient(callInvoker), logger)
{
    public async Task<InvitationResponse> CreateAsync(string email, long parentCtrlNbr, string role, int expirationDays = 7, long railroadCtrlNbr = 0)
    {
        try
        {
            return await _client.CreateInvitationAsync(new CreateInvitationRequest
            {
                Email = email,
                ParentCtrlNbr = parentCtrlNbr,
                Role = role,
                ExpirationDays = expirationDays,
                RailroadCtrlNbr = railroadCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetInvitationsResponse> GetByParentAsync(long parentCtrlNbr)
    {
        try
        {
            return await _client.GetInvitationsByParentAsync(new GetInvitationsByParentRequest
            {
                ParentCtrlNbr = parentCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<InvitationResponse> RevokeAsync(long ctrlNbr)
    {
        try
        {
            return await _client.RevokeInvitationAsync(new RevokeInvitationRequest
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

    public async Task<InvitationResponse> ResendAsync(long ctrlNbr)
    {
        try
        {
            return await _client.ResendInvitationAsync(new ResendInvitationRequest
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
