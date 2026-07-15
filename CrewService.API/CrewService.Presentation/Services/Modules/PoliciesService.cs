using CrewService.Application.Policies;
using CrewService.Application.Time;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;
using CrewService.Presentation.Services;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class PoliciesService(IServiceProvider serviceProvider) : PoliciesSrvc.PoliciesSrvcBase
{
    public override async Task<CraftOperationsPolicyResponse> GetCraftOperationsPolicy(
        GetCraftOperationsPolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var policy = await svc.GetCraftOperationsPolicyAsync(ControlNumber.Create(request.CraftCtrlNbr), context.CancellationToken);
            return MapCraftOperationsPolicy(policy);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<CraftOperationsPolicyResponse> UpsertCraftOperationsPolicy(
        UpsertCraftOperationsPolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var policy = await svc.GetOrUpsertCraftOperationsPolicyAsync(
                request.CraftCtrlNbr,
                request.HangoutAutoMoveEnabled,
                request.HangoutAutoMoveTargetBoardType,
                request.HangoutAutoMoveDelayHours,
                context.CancellationToken);
            return MapCraftOperationsPolicy(policy);
        }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    private static CraftOperationsPolicyResponse MapCraftOperationsPolicy(CraftOperationsPolicy p) => new()
    {
        CtrlNbr = p.CtrlNbr.Value,
        CraftCtrlNbr = p.CraftCtrlNbr.Value,
        HangoutAutoMoveEnabled = p.HangoutAutoMoveEnabled,
        HangoutAutoMoveTargetBoardType = p.HangoutAutoMoveTargetBoardType,
        HangoutAutoMoveDelayHours = p.HangoutAutoMoveDelayHours
    };

    public override async Task<DisplacementPolicyResponse> GetDisplacementPolicy(GetDisplacementPolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var policy = await svc.GetDisplacementPolicyAsync(ControlNumber.Create(request.CraftCtrlNbr), context.CancellationToken);
            return MapPolicy(policy);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<DisplacementPolicyResponse> UpsertDisplacementPolicy(UpsertDisplacementPolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var policy = await svc.GetOrUpsertDisplacementPolicyAsync(
            request.CraftCtrlNbr, request.WindowHours, request.SeniorityBasis,
            request.DefaultAction, request.EligibilitySelectorJson, context.CancellationToken);
        return MapPolicy(policy);
    }

    private static DisplacementPolicyResponse MapPolicy(CraftDisplacementPolicy p) => new()
    {
        CtrlNbr = p.CtrlNbr.Value,
        CraftCtrlNbr = p.CraftCtrlNbr.Value,
        WindowHours = p.WindowHours,
        SeniorityBasis = p.SeniorityBasis,
        DefaultAction = p.DefaultAction,
        EligibilitySelectorJson = p.EligibilitySelectorJson ?? string.Empty
    };

    public override async Task<BulletinPolicyResponse> GetBulletinPolicy(GetBulletinPolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var policy = await svc.GetBulletinPolicyAsync(ControlNumber.Create(request.CraftCtrlNbr), context.CancellationToken);
            return MapBulletinPolicy(policy);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<BulletinPolicyResponse> UpsertBulletinPolicy(UpsertBulletinPolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var policy = await svc.GetOrUpsertBulletinPolicyAsync(
            request.CraftCtrlNbr, request.BidWindowHours, request.ForcedAssignmentEnabled,
            request.ForcedAssignmentBasis, context.CancellationToken);
        return MapBulletinPolicy(policy);
    }

    public override async Task<CallSheetRuleResponse> GetCallSheetRule(GetCallSheetRuleRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var rule = await svc.GetCallSheetRuleAsync(ControlNumber.Create(request.DepartmentCtrlNbr), context.CancellationToken);
            return MapCallSheetRule(rule);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<CallSheetRuleResponse> UpsertCallSheetRule(UpsertCallSheetRuleRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var rule = await svc.GetOrUpsertCallSheetRuleAsync(
                request.DepartmentCtrlNbr,
                request.CallLeadMinutes,
                request.CallDurationMinutes,
                request.HolidayAdjustment,
                request.HolidayAdjustment.Equals(CallSheetHolidayAdjustmentType.CustomOffset, StringComparison.OrdinalIgnoreCase)
                    ? request.HolidayCustomOffsetMinutes
                    : null,
                request.GlobalPreCreateOffsetMinutes,
                request.IsEnabled,
                context.CancellationToken);

            return MapCallSheetRule(rule);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<DepartmentReassignmentRuleResponse> GetDepartmentReassignmentRule(
        GetDepartmentReassignmentRuleRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var rule = await svc.GetDepartmentReassignmentRuleAsync(ControlNumber.Create(request.DepartmentCtrlNbr), context.CancellationToken);
            return MapDepartmentReassignmentRule(rule);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<DepartmentReassignmentRuleResponse> UpsertDepartmentReassignmentRule(
        UpsertDepartmentReassignmentRuleRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var rule = await svc.GetOrUpsertDepartmentReassignmentRuleAsync(
                request.DepartmentCtrlNbr,
                request.TargetBoardType,
                request.IsRequired,
                context.CancellationToken);

            return MapDepartmentReassignmentRule(rule);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    private static BulletinPolicyResponse MapBulletinPolicy(BulletinPolicy p) => new()
    {
        CtrlNbr = p.CtrlNbr.Value,
        CraftCtrlNbr = p.CraftCtrlNbr.Value,
        BidWindowHours = p.BidWindowHours,
        ForcedAssignmentEnabled = p.ForcedAssignmentEnabled,
        ForcedAssignmentBasis = p.ForcedAssignmentBasis
    };

    private static CallSheetRuleResponse MapCallSheetRule(CallSheetRule r) => new()
    {
        CtrlNbr = r.CtrlNbr.Value,
        DepartmentCtrlNbr = r.DepartmentCtrlNbr.Value,
        CallLeadMinutes = r.CallLeadMinutes,
        CallDurationMinutes = r.CallDurationMinutes,
        HolidayAdjustment = r.HolidayAdjustment,
        HolidayCustomOffsetMinutes = r.HolidayCustomOffsetMinutes ?? 0,
        GlobalPreCreateOffsetMinutes = r.GlobalPreCreateOffsetMinutes,
        IsEnabled = r.IsEnabled
    };

    private static DepartmentReassignmentRuleResponse MapDepartmentReassignmentRule(DepartmentReassignmentRule r) => new()
    {
        CtrlNbr = r.CtrlNbr.Value,
        DepartmentCtrlNbr = r.DepartmentCtrlNbr.Value,
        TargetBoardType = r.TargetBoardType.ToString(),
        IsRequired = r.IsRequired
    };

    public override async Task<SeniorityMovePolicyResponse> GetSeniorityMovePolicy(GetSeniorityMovePolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var policy = await svc.GetSeniorityMovePolicyAsync(ControlNumber.Create(request.RailroadCtrlNbr), ControlNumber.Create(request.CraftCtrlNbr), context.CancellationToken);
            return MapSeniorityMovePolicy(policy);
        }
        catch (KeyNotFoundException)
        {
            // Missing policy is a valid configuration state for a craft.
            // Return an empty response (CtrlNbr == 0) so callers can treat it as "not configured"
            // without relying on exception flow.
            return new SeniorityMovePolicyResponse
            {
                RailroadCtrlNbr = request.RailroadCtrlNbr,
                CraftCtrlNbr = request.CraftCtrlNbr
            };
        }
    }

    public override async Task<SeniorityMovePolicyResponse> UpsertSeniorityMovePolicy(UpsertSeniorityMovePolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var policy = await svc.GetOrUpsertSeniorityMovePolicyAsync(
            request.RailroadCtrlNbr, request.CraftCtrlNbr, request.RequestHours,
            request.CancelHours, request.AutoApprove,
            request.CrewToCrewStrategy, request.CrewToBoardStrategy,
            request.ExtraBoardToCrewStrategy, request.HangoutToCrewStrategy,
            request.ExtendedAbsenceToCrewStrategy, request.TrainingToCrewStrategy,
            request.NewHireToCrewStrategy, request.WillWorkEnabled,
            request.CrewToCrewEligibilityDays, request.CrewToBoardEligibilityDays,
            request.ExtraBoardToCrewEligibilityDays, request.HangoutToCrewEligibilityDays,
            request.ExtendedAbsenceToCrewEligibilityDays, request.TrainingToCrewEligibilityDays,
            request.NewHireToCrewEligibilityDays,
            context.CancellationToken);
        return MapSeniorityMovePolicy(policy);
    }

    public override async Task<NoAccessPolicyResponse> GetNoAccessPolicy(GetNoAccessPolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var policy = await svc.GetNoAccessPolicyAsync(
                ControlNumber.Create(request.RailroadCtrlNbr),
                ControlNumber.Create(request.CraftCtrlNbr),
                context.CancellationToken);
            return MapNoAccessPolicy(policy);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<NoAccessPolicyResponse> UpsertNoAccessPolicy(UpsertNoAccessPolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var policy = await svc.GetOrUpsertNoAccessPolicyAsync(
            request.RailroadCtrlNbr,
            request.CraftCtrlNbr,
            request.IsEnabled,
            request.AllowEmployeeSelfRequest,
            request.RequireBulletinAccessAudit,
            request.BlockIfOnExtendedAbsence,
            request.RequirePositionCurrentlyAssigned,
            request.ApplyExtraBoardSpecialCase,
            request.RequireBoardAvailableForMoveOff,
            request.AutoApproveNoAccess,
            request.AllowAdminOverride,
            request.BlockIfEmployeeMarkedOff,
            request.BlockIfLastVacatedIncumbent,
            request.DefaultEffectiveMode,
            context.CancellationToken);

        return MapNoAccessPolicy(policy);
    }

    public override async Task<ListNoAccessPoliciesByRailroadResponse> ListNoAccessPoliciesByRailroad(
        ListNoAccessPoliciesByRailroadRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var items = await svc.ListNoAccessPoliciesByRailroadAsync(ControlNumber.Create(request.RailroadCtrlNbr), context.CancellationToken);

        var response = new ListNoAccessPoliciesByRailroadResponse();
        foreach (var item in items)
        {
            response.Items.Add(new NoAccessPolicyListItem
            {
                CraftCtrlNbr = item.Craft.CtrlNbr.Value,
                CraftName = item.Craft.CraftName,
                HasPolicy = item.Policy is not null,
                Policy = item.Policy is null ? new NoAccessPolicyResponse() : MapNoAccessPolicy(item.Policy)
            });
        }

        return response;
    }

    public override async Task<NoAccessPolicyResponse> CreateMissingNoAccessPolicy(
        CreateMissingNoAccessPolicyRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var policy = await svc.CreateMissingNoAccessPolicyAsync(
            request.RailroadCtrlNbr,
            request.CraftCtrlNbr,
            context.CancellationToken);
        return MapNoAccessPolicy(policy);
    }

    public override async Task<SeniorityMoveResponse> RequestNoAccessByBulletin(
        RequestNoAccessByBulletinRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var move = await svc.RequestNoAccessByBulletinAsync(
                request.RailroadCtrlNbr,
                request.CraftCtrlNbr,
                request.BulletinCtrlNbr,
                request.EmployeeCtrlNbr,
                request.AdminOverride,
                context.CancellationToken);
            return MapSeniorityMove(move);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    private static NoAccessPolicyResponse MapNoAccessPolicy(NoAccessPolicy p) => new()
    {
        CtrlNbr = p.CtrlNbr.Value,
        RailroadCtrlNbr = p.RailroadCtrlNbr.Value,
        CraftCtrlNbr = p.CraftCtrlNbr.Value,
        IsEnabled = p.IsEnabled,
        AllowEmployeeSelfRequest = p.AllowEmployeeSelfRequest,
        RequireBulletinAccessAudit = p.RequireBulletinAccessAudit,
        BlockIfOnExtendedAbsence = p.BlockIfOnExtendedAbsence,
        RequirePositionCurrentlyAssigned = p.RequirePositionCurrentlyAssigned,
        ApplyExtraBoardSpecialCase = p.ApplyExtraBoardSpecialCase,
        RequireBoardAvailableForMoveOff = p.RequireBoardAvailableForMoveOff,
        AutoApproveNoAccess = p.AutoApproveNoAccess,
        AllowAdminOverride = p.AllowAdminOverride,
        BlockIfEmployeeMarkedOff = p.BlockIfEmployeeMarkedOff,
        BlockIfLastVacatedIncumbent = p.BlockIfLastVacatedIncumbent,
        DefaultEffectiveMode = p.DefaultEffectiveMode
    };

    private static SeniorityMovePolicyResponse MapSeniorityMovePolicy(SeniorityMovePolicy p) => new()
    {
        CtrlNbr = p.CtrlNbr.Value,
        RailroadCtrlNbr = p.RailroadCtrlNbr.Value,
        CraftCtrlNbr = p.CraftCtrlNbr.Value,
        RequestHours = p.RequestHours,
        CancelHours = p.CancelHours,
        AutoApprove = p.AutoApprove,
        CrewToCrewStrategy = p.CrewToCrewStrategy,
        CrewToBoardStrategy = p.CrewToBoardStrategy,
        ExtraBoardToCrewStrategy = p.ExtraBoardToCrewStrategy,
        HangoutToCrewStrategy = p.HangoutToCrewStrategy,
        ExtendedAbsenceToCrewStrategy = p.ExtendedAbsenceToCrewStrategy,
        TrainingToCrewStrategy = p.TrainingToCrewStrategy,
        NewHireToCrewStrategy = p.NewHireToCrewStrategy,
        WillWorkEnabled = p.WillWorkEnabled,
        CrewToCrewEligibilityDays = p.CrewToCrewEligibilityDays,
        CrewToBoardEligibilityDays = p.CrewToBoardEligibilityDays,
        ExtraBoardToCrewEligibilityDays = p.ExtraBoardToCrewEligibilityDays,
        HangoutToCrewEligibilityDays = p.HangoutToCrewEligibilityDays,
        ExtendedAbsenceToCrewEligibilityDays = p.ExtendedAbsenceToCrewEligibilityDays,
        TrainingToCrewEligibilityDays = p.TrainingToCrewEligibilityDays,
        NewHireToCrewEligibilityDays = p.NewHireToCrewEligibilityDays
    };

    public override async Task<SeniorityMoveResponse> ExerciseSeniorityMove(ExerciseSeniorityMoveRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var move = await svc.ExerciseSeniorityMoveAsync(
                request.RailroadCtrlNbr, request.EmployeeCtrlNbr, request.CraftCtrlNbr, request.TargetPositionCtrlNbr,
                request.DisplacedEmployeeCtrlNbr == 0 ? null : request.DisplacedEmployeeCtrlNbr,
                request.DaysOnCurrentPosition,
                string.IsNullOrEmpty(request.MoveType) ? SeniorityMoveType.Voluntary : request.MoveType,
                request.TargetBoardCtrlNbr,
                request.HasWillWork ? request.WillWork : null,
                context.CancellationToken);
            return MapSeniorityMove(move);
        }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<PreviewSeniorityMoveEffectiveDateResponse> PreviewSeniorityMoveEffectiveDate(
        PreviewSeniorityMoveEffectiveDateRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var (effectiveUtc, willWorkOffered) = await svc.PreviewEffectiveDateWithWillWorkAsync(
                request.RailroadCtrlNbr, request.EmployeeCtrlNbr, request.CraftCtrlNbr,
                request.TargetPositionCtrlNbr, request.TargetBoardCtrlNbr,
                context.CancellationToken);

            var tz = await ResolveCraftTimeZoneAsync(request.CraftCtrlNbr, context.CancellationToken);
            return new PreviewSeniorityMoveEffectiveDateResponse
            {
                // Presentation field now carries work-area-localized wall clock emitted by backend.
                EffectiveLocal = serviceProvider.GetRequiredService<IWorkAreaClock>()
                    .FormatLocalIso(effectiveUtc.UtcDateTime, tz),
                WillWorkOffered = willWorkOffered
            };
        }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<SeniorityMoveResponse> ApproveSeniorityMove(ApproveSeniorityMoveRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        DateTime? effectiveUtc = string.IsNullOrEmpty(request.EffectiveUtc)
            ? null
            : DateTime.Parse(request.EffectiveUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);
        try
        {
            var move = await svc.ApproveSeniorityMoveAsync(ControlNumber.Create(request.MoveCtrlNbr), effectiveUtc, context.CancellationToken);
            return MapSeniorityMove(move);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<SeniorityMoveResponse> RejectSeniorityMove(RejectSeniorityMoveRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var move = await svc.RejectSeniorityMoveAsync(ControlNumber.Create(request.MoveCtrlNbr), request.Reason, context.CancellationToken);
            return MapSeniorityMove(move);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<SeniorityMoveResponse> CancelSeniorityMove(CancelSeniorityMoveRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var move = await svc.CancelSeniorityMoveAsync(ControlNumber.Create(request.MoveCtrlNbr), request.Reason, context.CancellationToken);
            return MapSeniorityMove(move);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<SeniorityMoveResponse> CompleteSeniorityMove(CompleteSeniorityMoveRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var move = await svc.CompleteSeniorityMoveAsync(ControlNumber.Create(request.MoveCtrlNbr), context.CancellationToken);
            return MapSeniorityMove(move);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<GetSeniorityMovesResponse> GetSeniorityMovesByEmployee(GetSeniorityMovesByEmployeeRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var moves = await svc.GetSeniorityMovesByEmployeeAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
        return await BuildMovesResponseAsync(moves, context.CancellationToken);
    }

    public override async Task<GetSeniorityMovesResponse> GetSeniorityMovesByCraft(GetSeniorityMovesByCraftRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var moves = await svc.GetSeniorityMovesByCraftAsync(
            ControlNumber.Create(request.CraftCtrlNbr),
            string.IsNullOrEmpty(request.Status) ? null : request.Status,
            context.CancellationToken);
        return await BuildMovesResponseAsync(moves, context.CancellationToken);
    }

    public override async Task<GetSeniorityMovesResponse> GetPendingSeniorityMoves(GetPendingSeniorityMovesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var moves = await svc.GetPendingSeniorityMovesAsync(context.CancellationToken);
        return await BuildMovesResponseAsync(moves, context.CancellationToken);
    }

    public override async Task<GetSeniorityMovesResponse> GetActiveSeniorityMoves(GetActiveSeniorityMovesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var moves = await svc.GetActiveSeniorityMovesAsync(context.CancellationToken);
        return await BuildMovesResponseAsync(moves, context.CancellationToken);
    }

    public override async Task<GetSeniorityMovesResponse> GetAllSeniorityMoves(GetAllSeniorityMovesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var moves = await svc.GetAllSeniorityMovesAsync(context.CancellationToken);
        return await BuildMovesResponseAsync(moves, context.CancellationToken);
    }

    public override async Task<GetNextSeniorityMoveEventResponse> GetNextSeniorityMoveEvent(
        GetNextSeniorityMoveEventRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var clock = serviceProvider.GetRequiredService<IWorkAreaClock>();

        var moves = await svc.GetActiveSeniorityMovesAsync(context.CancellationToken);
        var nowUtc = clock.UtcNow.UtcDateTime;
        var next = moves
            .Where(m => m.Move.EffectiveUtc.HasValue && m.Move.EffectiveUtc.Value >= nowUtc)
            .OrderBy(m => m.Move.EffectiveUtc)
            .FirstOrDefault();

        if (next is null)
            return new GetNextSeniorityMoveEventResponse { NextEventLocal = string.Empty };

        var tz = clock.ResolveTimeZone(next.WorkAreaTimeZoneId);
        return new GetNextSeniorityMoveEventResponse
        {
            NextEventLocal = clock.FormatLocalIso(next.Move.EffectiveUtc!.Value, tz)
        };
    }

    private async Task<GetSeniorityMovesResponse> BuildMovesResponseAsync(
        IReadOnlyList<SeniorityMoveListItem> moves, CancellationToken ct)
    {
        var nameService = serviceProvider.GetRequiredService<EmployeeNameService>();
        var clock = serviceProvider.GetRequiredService<IWorkAreaClock>();
        var employeeNames = await nameService.GetEmployeeInfoBatchAsync(moves.Select(m => m.Move.EmployeeCtrlNbr));

        var response = new GetSeniorityMovesResponse { TotalCount = moves.Count };
        foreach (var item in moves)
        {
            var mapped = MapSeniorityMove(item.Move);
            mapped.AutoApprove = item.AutoApprove;
            mapped.TargetPositionName = item.TargetPositionName;

            // Re-render the UTC instants as work-area-local, offset-carrying ISO strings
            // (e.g. "...-05:00") so the UI displays the correct work-area wall clock.
            var tz = clock.ResolveTimeZone(item.WorkAreaTimeZoneId);
            mapped.RequestedLocal = clock.FormatLocalIso(item.Move.RequestedUtc, tz);
            if (item.Move.EffectiveUtc.HasValue)
                mapped.EffectiveLocal = clock.FormatLocalIso(item.Move.EffectiveUtc.Value, tz);
            if (employeeNames.TryGetValue(item.Move.EmployeeCtrlNbr, out var info))
            {
                var number = string.IsNullOrWhiteSpace(info.EmployeeNumber)
                    ? string.Empty
                    : $" ({info.EmployeeNumber.ToUpperInvariant()})";
                mapped.EmployeeName = $"{info.FullNameLnf}{number}".Trim();
            }
            response.Moves.Add(mapped);
        }
        return response;
    }

    private static SeniorityMoveResponse MapSeniorityMove(SeniorityMove m)
    {
        var response = new SeniorityMoveResponse
        {
            CtrlNbr = m.CtrlNbr.Value,
            RailroadCtrlNbr = m.RailroadCtrlNbr.Value,
            EmployeeCtrlNbr = m.EmployeeCtrlNbr.Value,
            CraftCtrlNbr = m.CraftCtrlNbr.Value,
            TargetPositionCtrlNbr = m.TargetPositionCtrlNbr.Value,
            DisplacedEmployeeCtrlNbr = m.DisplacedEmployeeCtrlNbr?.Value ?? 0,
            RequestedLocal = m.RequestedUtc.ToString("O"),
            EffectiveLocal = m.EffectiveUtc?.ToString("O") ?? string.Empty,
            DaysOnCurrentPosition = m.DaysOnCurrentPosition,
            MoveType = m.MoveType,
            Status = m.Status,
            RejectionReason = m.RejectionReason ?? string.Empty,
            CancellationReason = m.CancellationReason ?? string.Empty
        };
        if (m.WillWork.HasValue)
            response.WillWork = m.WillWork.Value;
        return response;
    }

    private async Task<TimeZoneInfo?> ResolveCraftTimeZoneAsync(long craftCtrlNbr, CancellationToken ct)
    {
        if (craftCtrlNbr <= 0) return null;

        var uowFactory = serviceProvider.GetRequiredService<IOrchestrationUnitOfWorkFactory>();
        var clock = serviceProvider.GetRequiredService<IWorkAreaClock>();

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var rosters = await uow.Rosters.GetByCraftCtrlNbrAsync(ControlNumber.Create(craftCtrlNbr));
        var roster = rosters.FirstOrDefault();
        if (roster is null) return null;

        var group = await uow.DynamicGroups.GetByCtrlNbrAsync(roster.WorkAreaGroupCtrlNbr, ct);
        return clock.ResolveTimeZone(group?.TimeZoneId);
    }
}