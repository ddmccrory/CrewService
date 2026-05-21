using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class BulletinsClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<BulletinsClient> logger)
    : BaseGrpcClient<BulletinsSrvc.BulletinsSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new BulletinsSrvc.BulletinsSrvcClient(callInvoker), logger)
{
    // ── Vacancies ──────────────────────────────────────────────────────

    public async Task<GetVacanciesResponse> GetOpenVacanciesAsync(long railroadCtrlNbr = 0)
    {
        try { return await _client.GetOpenVacanciesAsync(new GetOpenVacanciesRequest { RailroadCtrlNbr = railroadCtrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetVacanciesResponse> GetVacanciesByWorkAreaAsync(long workAreaCtrlNbr)
    {
        try { return await _client.GetVacanciesByWorkAreaAsync(new GetVacanciesByWorkAreaRequest { WorkAreaCtrlNbr = workAreaCtrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<PositionVacancyResponse> GetVacancyAsync(long ctrlNbr)
    {
        try { return await _client.GetVacancyAsync(new GetVacancyRequest { CtrlNbr = ctrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<OpenVacancyResponse> OpenVacancyAsync(OpenVacancyRequest request)
    {
        try { return await _client.OpenVacancyAsync(request); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<PositionVacancyResponse> AbolishVacancyAsync(long ctrlNbr)
    {
        try { return await _client.AbolishVacancyAsync(new AbolishVacancyRequest { CtrlNbr = ctrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    // ── Bulletins ──────────────────────────────────────────────────────

    public async Task<GetBulletinsResponse> GetActiveBulletinsAsync(long railroadCtrlNbr = 0)
    {
        try { return await _client.GetActiveBulletinsAsync(new GetActiveBulletinsRequest { RailroadCtrlNbr = railroadCtrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetBulletinsResponse> GetBulletinsInDateRangeAsync(DateTime fromUtc, long railroadCtrlNbr = 0)
    {
        try { return await _client.GetBulletinsInDateRangeAsync(new GetBulletinsInDateRangeRequest { RailroadCtrlNbr = railroadCtrlNbr, FromUtc = fromUtc.ToString("O") }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetBulletinsResponse> GetPostedBulletinsAsync(long railroadCtrlNbr = 0)
    {
        try { return await _client.GetPostedBulletinsAsync(new GetPostedBulletinsRequest { RailroadCtrlNbr = railroadCtrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetBulletinsResponse> GetBulletinsByWorkAreaAsync(long workAreaCtrlNbr)
    {
        try { return await _client.GetBulletinsByWorkAreaAsync(new GetBulletinsByWorkAreaRequest { WorkAreaCtrlNbr = workAreaCtrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<BulletinResponse> GetBulletinAsync(long ctrlNbr)
    {
        try { return await _client.GetBulletinAsync(new GetBulletinRequest { CtrlNbr = ctrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<BulletinResponse> PostBulletinForVacancyAsync(PostBulletinForVacancyRequest request)
    {
        try { return await _client.PostBulletinForVacancyAsync(request); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<BulletinResponse> AwardBulletinAsync(long ctrlNbr, long employeeCtrlNbr)
    {
        try { return await _client.AwardBulletinAsync(new AwardBulletinRequest { CtrlNbr = ctrlNbr, EmployeeCtrlNbr = employeeCtrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<BulletinResponse> ForceAssignBulletinAsync(long ctrlNbr, long employeeCtrlNbr)
    {
        try { return await _client.ForceAssignBulletinAsync(new ForceAssignBulletinRequest { CtrlNbr = ctrlNbr, EmployeeCtrlNbr = employeeCtrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<BulletinResponse> AutoForceAssignBulletinAsync(long ctrlNbr)
    {
        try { return await _client.AutoForceAssignBulletinAsync(new AutoForceAssignBulletinRequest { CtrlNbr = ctrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<BulletinResponse> SetNoBidAsync(long ctrlNbr)
    {
        try { return await _client.SetBulletinNoBidAsync(new SetBulletinNoBidRequest { CtrlNbr = ctrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    // ── Bids ───────────────────────────────────────────────────────────

    public async Task<BulletinBidResponse> SubmitBidAsync(long bulletinCtrlNbr, long employeeCtrlNbr, int priority)
    {
        try
        {
            return await _client.SubmitBidAsync(new SubmitBidRequest
            {
                BulletinCtrlNbr = bulletinCtrlNbr,
                EmployeeCtrlNbr = employeeCtrlNbr,
                Priority = priority
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<BulletinBidResponse> WithdrawBidAsync(long ctrlNbr)
    {
        try { return await _client.WithdrawBidAsync(new WithdrawBidRequest { CtrlNbr = ctrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetBidsResponse> GetBidsByBulletinAsync(long bulletinCtrlNbr)
    {
        try { return await _client.GetBidsByBulletinAsync(new GetBidsByBulletinRequest { BulletinCtrlNbr = bulletinCtrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetBidsResponse> GetBidsByEmployeeAsync(long employeeCtrlNbr)
    {
        try { return await _client.GetBidsByEmployeeAsync(new GetBidsByEmployeeRequest { EmployeeCtrlNbr = employeeCtrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    // ── BulletinRules ──────────────────────────────────────────────────

    public async Task<BulletinRuleResponse?> GetBulletinRuleAsync(long craftCtrlNbr)
    {
        try { return await _client.GetBulletinRuleAsync(new GetBulletinRuleRequest { CraftCtrlNbr = craftCtrlNbr }); }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound) { return null; }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<BulletinRuleResponse> SaveBulletinRuleAsync(SaveBulletinRuleRequest request)
    {
        try { return await _client.SaveBulletinRuleAsync(request); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetNextBulletinEventResponse?> GetNextBulletinEventUtcAsync(long railroadCtrlNbr = 0)
    {
        try { return await _client.GetNextBulletinEventAsync(new GetNextBulletinEventRequest { RailroadCtrlNbr = railroadCtrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }
}
