using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class PoliciesClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<PoliciesClient> logger)
    : BaseGrpcClient<PoliciesSrvc.PoliciesSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new PoliciesSrvc.PoliciesSrvcClient(callInvoker), logger)
{
    // ── Seniority Move Policy ─────────────────────────────────────────

    /// <summary>
    /// Returns the configured seniority move policy for the given railroad/craft, or
    /// <c>null</c> when none has been configured. A missing policy is an expected business
    /// state (not every craft has one), so <see cref="StatusCode.NotFound"/> is returned as
    /// <c>null</c> rather than logged and thrown as an error.
    /// </summary>
    public async Task<SeniorityMovePolicyResponse?> GetSeniorityMovePolicyAsync(long railroadCtrlNbr, long craftCtrlNbr)
    {
        try { return await _client.GetSeniorityMovePolicyAsync(new GetSeniorityMovePolicyRequest { RailroadCtrlNbr = railroadCtrlNbr, CraftCtrlNbr = craftCtrlNbr }); }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) { return null; }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<SeniorityMovePolicyResponse> UpsertSeniorityMovePolicyAsync(
        long railroadCtrlNbr, long craftCtrlNbr,
        int eligibilityDays, int requestHours, int cancelHours, bool autoApprove,
        string crewToCrewStrategy, string crewToBoardStrategy,
        string extraBoardToCrewStrategy, string hangoutToCrewStrategy,
        string extendedAbsenceToCrewStrategy, string trainingToCrewStrategy,
        string newHireToCrewStrategy, bool willWorkEnabled = false)
    {
        try
        {
            return await _client.UpsertSeniorityMovePolicyAsync(new UpsertSeniorityMovePolicyRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr,
                CraftCtrlNbr = craftCtrlNbr,
                EligibilityDays = eligibilityDays,
                RequestHours = requestHours,
                CancelHours = cancelHours,
                AutoApprove = autoApprove,
                CrewToCrewStrategy = crewToCrewStrategy,
                CrewToBoardStrategy = crewToBoardStrategy,
                ExtraBoardToCrewStrategy = extraBoardToCrewStrategy,
                HangoutToCrewStrategy = hangoutToCrewStrategy,
                ExtendedAbsenceToCrewStrategy = extendedAbsenceToCrewStrategy,
                TrainingToCrewStrategy = trainingToCrewStrategy,
                NewHireToCrewStrategy = newHireToCrewStrategy,
                WillWorkEnabled = willWorkEnabled
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    // ── Seniority Moves ───────────────────────────────────────────────

    public async Task<SeniorityMoveResponse> ExerciseSeniorityMoveAsync(ExerciseSeniorityMoveRequest request)
    {
        try { return await _client.ExerciseSeniorityMoveAsync(request); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<PreviewSeniorityMoveEffectiveDateResponse> PreviewSeniorityMoveEffectiveDateAsync(PreviewSeniorityMoveEffectiveDateRequest request)
    {
        try { return await _client.PreviewSeniorityMoveEffectiveDateAsync(request); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<SeniorityMoveResponse> ApproveSeniorityMoveAsync(long ctrlNbr, string? effectiveUtc = null)
    {
        try
        {
            var req = new ApproveSeniorityMoveRequest { MoveCtrlNbr = ctrlNbr };
            if (effectiveUtc is not null) req.EffectiveUtc = effectiveUtc;
            return await _client.ApproveSeniorityMoveAsync(req);
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<SeniorityMoveResponse> RejectSeniorityMoveAsync(long ctrlNbr, string reason)
    {
        try { return await _client.RejectSeniorityMoveAsync(new RejectSeniorityMoveRequest { MoveCtrlNbr = ctrlNbr, Reason = reason }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<SeniorityMoveResponse> CancelSeniorityMoveAsync(long ctrlNbr, string reason)
    {
        try { return await _client.CancelSeniorityMoveAsync(new CancelSeniorityMoveRequest { MoveCtrlNbr = ctrlNbr, Reason = reason }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<SeniorityMoveResponse> CompleteSeniorityMoveAsync(long ctrlNbr)
    {
        try { return await _client.CompleteSeniorityMoveAsync(new CompleteSeniorityMoveRequest { MoveCtrlNbr = ctrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetSeniorityMovesResponse> GetMovesByEmployeeAsync(long employeeCtrlNbr)
    {
        try { return await _client.GetSeniorityMovesByEmployeeAsync(new GetSeniorityMovesByEmployeeRequest { EmployeeCtrlNbr = employeeCtrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetSeniorityMovesResponse> GetMovesByCraftAsync(long craftCtrlNbr, string status = "")
    {
        try { return await _client.GetSeniorityMovesByCraftAsync(new GetSeniorityMovesByCraftRequest { CraftCtrlNbr = craftCtrlNbr, Status = status }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetSeniorityMovesResponse> GetPendingMovesAsync()
    {
        try { return await _client.GetPendingSeniorityMovesAsync(new GetPendingSeniorityMovesRequest()); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetSeniorityMovesResponse> GetActiveMovesAsync()
    {
        try { return await _client.GetActiveSeniorityMovesAsync(new GetActiveSeniorityMovesRequest()); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetSeniorityMovesResponse> GetAllMovesAsync()
    {
        try { return await _client.GetAllSeniorityMovesAsync(new GetAllSeniorityMovesRequest()); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<GetNextSeniorityMoveEventResponse?> GetNextSeniorityMoveEventLocalAsync()
    {
        try { return await _client.GetNextSeniorityMoveEventAsync(new GetNextSeniorityMoveEventRequest()); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    /// <summary>
    /// Returns the subset of craft ctrl nbrs (from <paramref name="craftCtrlNbrs"/>) that have
    /// a configured seniority move policy. Crafts with no policy are silently omitted.
    /// </summary>
    public async Task<HashSet<long>> GetCraftsWithSeniorityMovePolicyAsync(long railroadCtrlNbr, IEnumerable<long> craftCtrlNbrs)
    {
        var tasks = craftCtrlNbrs.Select(async ctrlNbr =>
        {
            try
            {
                var policy = await _client.GetSeniorityMovePolicyAsync(new GetSeniorityMovePolicyRequest { RailroadCtrlNbr = railroadCtrlNbr, CraftCtrlNbr = ctrlNbr });
                // A policy with ctrl_nbr == 0 means it was not found / not configured
                return policy.CtrlNbr > 0 ? ctrlNbr : (long?)null;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                return (long?)null;
            }
            catch
            {
                return (long?)null;
            }
        });

        var results = await Task.WhenAll(tasks);
        return [.. results.Where(r => r.HasValue).Select(r => r!.Value)];
    }
}
