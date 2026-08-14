using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class PoliciesClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<PoliciesClient> logger)
    : BaseGrpcClient<PoliciesSrvc.PoliciesSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new PoliciesSrvc.PoliciesSrvcClient(callInvoker), logger)
{
    // ── Absence Approval Policy ───────────────────────────────────────

    public async Task<AbsenceApprovalPolicyResponse?> GetAbsenceApprovalPolicyAsync(long railroadCtrlNbr)
    {
        try
        {
            var response = await _client.GetAbsenceApprovalPolicyAsync(new GetAbsenceApprovalPolicyRequest { RailroadCtrlNbr = railroadCtrlNbr });
            return response.CtrlNbr > 0 ? response : null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) { return null; }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<AbsenceApprovalPolicyResponse> UpsertAbsenceApprovalPolicyAsync(
        long railroadCtrlNbr,
        string approvalLevel,
        bool isEnabled,
        bool autoMarkOffIfWithinHoursEnabled,
        int autoMarkOffIfWithinHours)
    {
        try
        {
            return await _client.UpsertAbsenceApprovalPolicyAsync(new UpsertAbsenceApprovalPolicyRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr,
                ApprovalLevel = approvalLevel,
                IsEnabled = isEnabled,
                AutoMarkOffIfWithinHoursEnabled = autoMarkOffIfWithinHoursEnabled,
                AutoMarkOffIfWithinHours = autoMarkOffIfWithinHours
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<DepartmentAbsenceRequestWindowPolicyResponse?> GetDepartmentAbsenceRequestWindowPolicyAsync(long departmentCtrlNbr)
    {
        try
        {
            var response = await _client.GetDepartmentAbsenceRequestWindowPolicyAsync(
                new GetDepartmentAbsenceRequestWindowPolicyRequest { DepartmentCtrlNbr = departmentCtrlNbr });
            return response.CtrlNbr > 0 ? response : null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) { return null; }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<DepartmentAbsenceRequestWindowPolicyResponse> UpsertDepartmentAbsenceRequestWindowPolicyAsync(
        long departmentCtrlNbr,
        int requestWindowCapDays)
    {
        try
        {
            return await _client.UpsertDepartmentAbsenceRequestWindowPolicyAsync(
                new UpsertDepartmentAbsenceRequestWindowPolicyRequest
                {
                    DepartmentCtrlNbr = departmentCtrlNbr,
                    RequestWindowCapDays = requestWindowCapDays
                });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CraftAbsenceWaitListPolicyResponse?> GetCraftAbsenceWaitListPolicyAsync(long craftCtrlNbr)
    {
        try
        {
            var response = await _client.GetCraftAbsenceWaitListPolicyAsync(
                new GetCraftAbsenceWaitListPolicyRequest { CraftCtrlNbr = craftCtrlNbr });
            return response.CtrlNbr > 0 ? response : null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) { return null; }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CraftAbsenceWaitListPolicyResponse> UpsertCraftAbsenceWaitListPolicyAsync(
        long craftCtrlNbr,
        int compensableDayMaxAssignments,
        int vacationWeekMaxAssignments,
        bool isEnabled)
    {
        try
        {
            return await _client.UpsertCraftAbsenceWaitListPolicyAsync(
                new UpsertCraftAbsenceWaitListPolicyRequest
                {
                    CraftCtrlNbr = craftCtrlNbr,
                    CompensableDayMaxAssignments = compensableDayMaxAssignments,
                    VacationWeekMaxAssignments = vacationWeekMaxAssignments,
                    IsEnabled = isEnabled
                });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    // ── Call Sheet Rules ────────────────────────────────────────────────

    public async Task<CallSheetRuleResponse?> GetCallSheetRuleAsync(long departmentCtrlNbr)
    {
        try { return await _client.GetCallSheetRuleAsync(new GetCallSheetRuleRequest { DepartmentCtrlNbr = departmentCtrlNbr }); }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) { return null; }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CallSheetRuleResponse> UpsertCallSheetRuleAsync(
        long departmentCtrlNbr,
        int callLeadMinutes,
        int callDurationMinutes,
        string holidayAdjustment,
        int? holidayCustomOffsetMinutes,
        int globalPreCreateOffsetMinutes,
        bool isEnabled)
    {
        try
        {
            var request = new UpsertCallSheetRuleRequest
            {
                DepartmentCtrlNbr = departmentCtrlNbr,
                CallLeadMinutes = callLeadMinutes,
                CallDurationMinutes = callDurationMinutes,
                HolidayAdjustment = holidayAdjustment,
                HolidayCustomOffsetMinutes = holidayCustomOffsetMinutes ?? 0,
                GlobalPreCreateOffsetMinutes = globalPreCreateOffsetMinutes,
                IsEnabled = isEnabled
            };

            return await _client.UpsertCallSheetRuleAsync(request);
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CraftCallSheetRuleResponse?> GetCraftCallSheetRuleAsync(long craftCtrlNbr)
    {
        try { return await _client.GetCraftCallSheetRuleAsync(new GetCraftCallSheetRuleRequest { CraftCtrlNbr = craftCtrlNbr }); }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) { return null; }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<CraftCallSheetRuleResponse> UpsertCraftCallSheetRuleAsync(
        long craftCtrlNbr,
        bool isEnabled,
        int preOnDutyChangeCutoffMinutes)
    {
        try
        {
            return await _client.UpsertCraftCallSheetRuleAsync(new UpsertCraftCallSheetRuleRequest
            {
                CraftCtrlNbr = craftCtrlNbr,
                IsEnabled = isEnabled,
                PreOnDutyChangeCutoffMinutes = preOnDutyChangeCutoffMinutes
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    // ── Department Reassignment Rules ────────────────────────────────────

    public async Task<DepartmentReassignmentRuleResponse?> GetDepartmentReassignmentRuleAsync(long departmentCtrlNbr)
    {
        try { return await _client.GetDepartmentReassignmentRuleAsync(new GetDepartmentReassignmentRuleRequest { DepartmentCtrlNbr = departmentCtrlNbr }); }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) { return null; }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<DepartmentReassignmentRuleResponse> UpsertDepartmentReassignmentRuleAsync(
        long departmentCtrlNbr,
        string targetBoardType,
        bool isRequired)
    {
        try
        {
            return await _client.UpsertDepartmentReassignmentRuleAsync(new UpsertDepartmentReassignmentRuleRequest
            {
                DepartmentCtrlNbr = departmentCtrlNbr,
                TargetBoardType = targetBoardType,
                IsRequired = isRequired
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

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
        int requestHours, int cancelHours, bool autoApprove,
        string crewToCrewStrategy, string crewToBoardStrategy,
        string extraBoardToCrewStrategy, string hangoutToCrewStrategy,
        string extendedAbsenceToCrewStrategy, string trainingToCrewStrategy,
        string newHireToCrewStrategy, bool willWorkEnabled = false,
        int crewToCrewEligibilityDays = 0, int crewToBoardEligibilityDays = 0,
        int extraBoardToCrewEligibilityDays = 0, int hangoutToCrewEligibilityDays = 0,
        int extendedAbsenceToCrewEligibilityDays = 0, int trainingToCrewEligibilityDays = 0,
        int newHireToCrewEligibilityDays = 0,
        bool allowScheduledHangoutMoves = false)
    {
        try
        {
            return await _client.UpsertSeniorityMovePolicyAsync(new UpsertSeniorityMovePolicyRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr,
                CraftCtrlNbr = craftCtrlNbr,
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
                WillWorkEnabled = willWorkEnabled,
                CrewToCrewEligibilityDays = crewToCrewEligibilityDays,
                CrewToBoardEligibilityDays = crewToBoardEligibilityDays,
                ExtraBoardToCrewEligibilityDays = extraBoardToCrewEligibilityDays,
                HangoutToCrewEligibilityDays = hangoutToCrewEligibilityDays,
                ExtendedAbsenceToCrewEligibilityDays = extendedAbsenceToCrewEligibilityDays,
                TrainingToCrewEligibilityDays = trainingToCrewEligibilityDays,
                NewHireToCrewEligibilityDays = newHireToCrewEligibilityDays,
                AllowScheduledHangoutMoves = allowScheduledHangoutMoves
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    // ── No Access Policy ─────────────────────────────────────────────

    public async Task<NoAccessPolicyResponse?> GetNoAccessPolicyAsync(long railroadCtrlNbr, long craftCtrlNbr)
    {
        try
        {
            return await _client.GetNoAccessPolicyAsync(new GetNoAccessPolicyRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr,
                CraftCtrlNbr = craftCtrlNbr
            });
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) { return null; }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<NoAccessPolicyResponse> UpsertNoAccessPolicyAsync(
        long railroadCtrlNbr,
        long craftCtrlNbr,
        bool isEnabled,
        bool allowEmployeeSelfRequest,
        bool requireBulletinAccessAudit,
        bool blockIfOnExtendedAbsence,
        bool requirePositionCurrentlyAssigned,
        bool applyExtraBoardSpecialCase,
        bool requireBoardAvailableForMoveOff,
        bool autoApproveNoAccess,
        bool allowAdminOverride,
        bool blockIfEmployeeMarkedOff,
        bool blockIfLastVacatedIncumbent,
        string defaultEffectiveMode)
    {
        try
        {
            return await _client.UpsertNoAccessPolicyAsync(new UpsertNoAccessPolicyRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr,
                CraftCtrlNbr = craftCtrlNbr,
                IsEnabled = isEnabled,
                AllowEmployeeSelfRequest = allowEmployeeSelfRequest,
                RequireBulletinAccessAudit = requireBulletinAccessAudit,
                BlockIfOnExtendedAbsence = blockIfOnExtendedAbsence,
                RequirePositionCurrentlyAssigned = requirePositionCurrentlyAssigned,
                ApplyExtraBoardSpecialCase = applyExtraBoardSpecialCase,
                RequireBoardAvailableForMoveOff = requireBoardAvailableForMoveOff,
                AutoApproveNoAccess = autoApproveNoAccess,
                AllowAdminOverride = allowAdminOverride,
                BlockIfEmployeeMarkedOff = blockIfEmployeeMarkedOff,
                BlockIfLastVacatedIncumbent = blockIfLastVacatedIncumbent,
                DefaultEffectiveMode = defaultEffectiveMode
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<ListNoAccessPoliciesByRailroadResponse> ListNoAccessPoliciesByRailroadAsync(long railroadCtrlNbr)
    {
        try { return await _client.ListNoAccessPoliciesByRailroadAsync(new ListNoAccessPoliciesByRailroadRequest { RailroadCtrlNbr = railroadCtrlNbr }); }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<NoAccessPolicyResponse> CreateMissingNoAccessPolicyAsync(long railroadCtrlNbr, long craftCtrlNbr)
    {
        try
        {
            return await _client.CreateMissingNoAccessPolicyAsync(new CreateMissingNoAccessPolicyRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr,
                CraftCtrlNbr = craftCtrlNbr
            });
        }
        catch (Exception ex) { LogException(ex); throw; }
    }

    public async Task<SeniorityMoveResponse> RequestNoAccessByBulletinAsync(
        long railroadCtrlNbr,
        long craftCtrlNbr,
        long bulletinCtrlNbr,
        long employeeCtrlNbr,
        bool adminOverride)
    {
        try
        {
            return await _client.RequestNoAccessByBulletinAsync(new RequestNoAccessByBulletinRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr,
                CraftCtrlNbr = craftCtrlNbr,
                BulletinCtrlNbr = bulletinCtrlNbr,
                EmployeeCtrlNbr = employeeCtrlNbr,
                AdminOverride = adminOverride
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
