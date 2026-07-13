using CrewService.Application.BackgroundWorkers;
using CrewService.Application.Notifications;
using CrewService.Application.Staffing;
using CrewService.Application.Time;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;
using System.Linq;

namespace CrewService.Application.Policies;

public sealed class PoliciesService(IOrchestrationUnitOfWorkFactory uowFactory, ISeniorityMoveSignal seniorityMoveSignal, IWorkAreaClock workAreaClock, EmployeeNotificationService notifications, ICurrentUserService currentUserService, SeniorityMoveExecutionService seniorityMoveExecutionService)
{
    public async Task<CraftOperationsPolicy> GetCraftOperationsPolicyAsync(
        ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.CraftOperationsPolicies.GetByCraftAsync(craftCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Craft operations policy for craft {craftCtrlNbr} not found.");
    }

    public async Task<CraftOperationsPolicy> GetOrUpsertCraftOperationsPolicyAsync(
        long craftCtrlNbr,
        bool hangoutAutoMoveEnabled,
        string hangoutAutoMoveTargetBoardType,
        int hangoutAutoMoveDelayHours,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<BoardType>(hangoutAutoMoveTargetBoardType, ignoreCase: true, out var boardType))
            throw new InvalidOperationException($"Invalid Hangout auto-move target board type '{hangoutAutoMoveTargetBoardType}'.");

        if (hangoutAutoMoveDelayHours < 0)
            throw new InvalidOperationException("Hangout auto-move delay hours must be greater than or equal to 0.");

        var normalizedTargetBoardType = boardType.ToString();

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var craftCn = ControlNumber.Create(craftCtrlNbr);
        var existing = await uow.CraftOperationsPolicies.GetByCraftAsync(craftCn, ct);
        if (existing is not null)
        {
            existing.Update(
                hangoutAutoMoveEnabled: hangoutAutoMoveEnabled,
                hangoutAutoMoveTargetBoardType: normalizedTargetBoardType,
                hangoutAutoMoveDelayHours: hangoutAutoMoveDelayHours);
            await uow.CraftOperationsPolicies.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }

        var policy = CraftOperationsPolicy.Create(
            craftCn,
            hangoutAutoMoveEnabled: hangoutAutoMoveEnabled,
            hangoutAutoMoveTargetBoardType: normalizedTargetBoardType,
            hangoutAutoMoveDelayHours: hangoutAutoMoveDelayHours);
        await uow.CraftOperationsPolicies.AddAsync(policy, ct);
        await uow.CommitAsync(ct);
        return policy;
    }

    public async Task<CraftDisplacementPolicy> GetOrUpsertDisplacementPolicyAsync(
        long craftCtrlNbr, int windowHours, string seniorityBasis, string defaultAction,
        string? eligibilitySelectorJson, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.CraftDisplacementPolicies.GetByCraftAsync(ControlNumber.Create(craftCtrlNbr));
        if (existing is not null)
        {
            existing.Update(windowHours, seniorityBasis, defaultAction, eligibilitySelectorJson);
            await uow.CraftDisplacementPolicies.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }
        var policy = CraftDisplacementPolicy.Create(craftCtrlNbr, windowHours, seniorityBasis, defaultAction, eligibilitySelectorJson);
        await uow.CraftDisplacementPolicies.AddAsync(policy, ct);
        await uow.CommitAsync(ct);
        return policy;
    }

    public async Task<CraftDisplacementPolicy> GetDisplacementPolicyAsync(
        ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.CraftDisplacementPolicies.GetByCraftAsync(craftCtrlNbr)
            ?? throw new KeyNotFoundException($"Displacement policy for craft {craftCtrlNbr} not found.");
    }

    public async Task<BulletinPolicy> GetOrUpsertBulletinPolicyAsync(
        long craftCtrlNbr, int bidWindowHours, bool forcedAssignmentEnabled, string forcedAssignmentBasis,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.BulletinPolicies.GetByCraftAsync(ControlNumber.Create(craftCtrlNbr));
        if (existing is not null)
        {
            existing.Update(bidWindowHours, forcedAssignmentEnabled, forcedAssignmentBasis);
            await uow.BulletinPolicies.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }
        var policy = BulletinPolicy.Create(craftCtrlNbr, bidWindowHours, forcedAssignmentEnabled, forcedAssignmentBasis);
        await uow.BulletinPolicies.AddAsync(policy, ct);
        await uow.CommitAsync(ct);
        return policy;
    }

    public async Task<BulletinPolicy> GetBulletinPolicyAsync(
        ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.BulletinPolicies.GetByCraftAsync(craftCtrlNbr)
            ?? throw new KeyNotFoundException($"Bulletin policy for craft {craftCtrlNbr} not found.");
    }

    public async Task<CallSheetRule> GetOrUpsertCallSheetRuleAsync(
        long departmentCtrlNbr,
        int callLeadMinutes,
        int callDurationMinutes,
        string holidayAdjustment,
        int? holidayCustomOffsetMinutes,
        int globalPreCreateOffsetMinutes,
        bool isEnabled,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var department = await uow.Departments.GetByCtrlNbrAsync(ControlNumber.Create(departmentCtrlNbr), ct)
            ?? throw new KeyNotFoundException($"Department {departmentCtrlNbr} not found.");

        if (department.DynamicGroupCtrlNbr is null)
            throw new InvalidOperationException($"Department {departmentCtrlNbr} is not scoped to a railroad/work area.");

        if (!holidayAdjustment.Equals(CallSheetHolidayAdjustmentType.None, StringComparison.OrdinalIgnoreCase)
            && !holidayAdjustment.Equals(CallSheetHolidayAdjustmentType.SkipHoliday, StringComparison.OrdinalIgnoreCase)
            && !holidayAdjustment.Equals(CallSheetHolidayAdjustmentType.AddDay, StringComparison.OrdinalIgnoreCase)
            && !holidayAdjustment.Equals(CallSheetHolidayAdjustmentType.CustomOffset, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Invalid call sheet holiday adjustment '{holidayAdjustment}'.");
        }

        if (holidayAdjustment.Equals(CallSheetHolidayAdjustmentType.CustomOffset, StringComparison.OrdinalIgnoreCase)
            && !holidayCustomOffsetMinutes.HasValue)
        {
            throw new InvalidOperationException("Holiday custom offset minutes are required when HolidayAdjustment is CustomOffset.");
        }

        var normalizedHolidayAdjustment = holidayAdjustment.Trim();

        var existing = await uow.CallSheetRules.GetByDepartmentAsync(ControlNumber.Create(departmentCtrlNbr));
        if (existing is not null)
        {
            existing.Update(
                callLeadMinutes,
                callDurationMinutes,
                normalizedHolidayAdjustment,
                holidayCustomOffsetMinutes,
                globalPreCreateOffsetMinutes,
                isEnabled);
            await uow.CallSheetRules.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }

        var rule = CallSheetRule.Create(
            departmentCtrlNbr,
            callLeadMinutes,
            callDurationMinutes,
            normalizedHolidayAdjustment,
            holidayCustomOffsetMinutes,
            globalPreCreateOffsetMinutes,
            isEnabled);

        await uow.CallSheetRules.AddAsync(rule, ct);
        await uow.CommitAsync(ct);
        return rule;
    }

    public async Task<CallSheetRule> GetCallSheetRuleAsync(
        ControlNumber departmentCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.CallSheetRules.GetByDepartmentAsync(departmentCtrlNbr)
            ?? throw new KeyNotFoundException($"Call sheet rule for department {departmentCtrlNbr} not found.");
    }

    public async Task<DepartmentReassignmentRule> GetOrUpsertDepartmentReassignmentRuleAsync(
        long departmentCtrlNbr,
        string targetBoardType,
        bool isRequired,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<BoardType>(targetBoardType, ignoreCase: true, out var parsedBoardType))
            throw new InvalidOperationException($"Invalid target board type '{targetBoardType}'.");

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var department = await uow.Departments.GetByCtrlNbrAsync(ControlNumber.Create(departmentCtrlNbr), ct)
            ?? throw new KeyNotFoundException($"Department {departmentCtrlNbr} not found.");

        if (department.DynamicGroupCtrlNbr is null)
            throw new InvalidOperationException($"Department {departmentCtrlNbr} is not scoped to a railroad/work area.");

        var existing = await uow.DepartmentReassignmentRules.GetByDepartmentAsync(ControlNumber.Create(departmentCtrlNbr));
        if (existing is not null)
        {
            existing.Update(parsedBoardType, isRequired);
            await uow.DepartmentReassignmentRules.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }

        var rule = DepartmentReassignmentRule.Create(ControlNumber.Create(departmentCtrlNbr), parsedBoardType, isRequired);
        await uow.DepartmentReassignmentRules.AddAsync(rule, ct);
        await uow.CommitAsync(ct);
        return rule;
    }

    public async Task<DepartmentReassignmentRule> GetDepartmentReassignmentRuleAsync(
        ControlNumber departmentCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.DepartmentReassignmentRules.GetByDepartmentAsync(departmentCtrlNbr)
            ?? throw new KeyNotFoundException($"Department reassignment rule for department {departmentCtrlNbr} not found.");
    }

    public async Task<SeniorityMovePolicy> GetOrUpsertSeniorityMovePolicyAsync(
        long railroadCtrlNbr, long craftCtrlNbr, int requestHours, int cancelHours, bool autoApprove,
        string crewToCrewStrategy, string crewToBoardStrategy,
        string extraBoardToCrewStrategy, string hangoutToCrewStrategy,
        string extendedAbsenceToCrewStrategy, string trainingToCrewStrategy,
        string newHireToCrewStrategy, bool willWorkEnabled = false,
        int crewToCrewEligibilityDays = 0, int crewToBoardEligibilityDays = 0,
        int extraBoardToCrewEligibilityDays = 0, int hangoutToCrewEligibilityDays = 0,
        int extendedAbsenceToCrewEligibilityDays = 0, int trainingToCrewEligibilityDays = 0,
        int newHireToCrewEligibilityDays = 0,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(ControlNumber.Create(railroadCtrlNbr), ControlNumber.Create(craftCtrlNbr));
        if (existing is not null)
        {
            existing.Update(requestHours, cancelHours, autoApprove,
                crewToCrewStrategy, crewToBoardStrategy, extraBoardToCrewStrategy,
                hangoutToCrewStrategy, extendedAbsenceToCrewStrategy, trainingToCrewStrategy, newHireToCrewStrategy,
                willWorkEnabled,
                crewToCrewEligibilityDays, crewToBoardEligibilityDays,
                extraBoardToCrewEligibilityDays, hangoutToCrewEligibilityDays,
                extendedAbsenceToCrewEligibilityDays, trainingToCrewEligibilityDays,
                newHireToCrewEligibilityDays);
            await uow.SeniorityMovePolicies.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }
        var policy = SeniorityMovePolicy.Create(ControlNumber.Create(railroadCtrlNbr), ControlNumber.Create(craftCtrlNbr),
            requestHours, cancelHours, autoApprove,
            crewToCrewStrategy, crewToBoardStrategy, extraBoardToCrewStrategy,
            hangoutToCrewStrategy, extendedAbsenceToCrewStrategy, trainingToCrewStrategy, newHireToCrewStrategy,
            willWorkEnabled,
            crewToCrewEligibilityDays, crewToBoardEligibilityDays,
            extraBoardToCrewEligibilityDays, hangoutToCrewEligibilityDays,
            extendedAbsenceToCrewEligibilityDays, trainingToCrewEligibilityDays,
            newHireToCrewEligibilityDays);
        await uow.SeniorityMovePolicies.AddAsync(policy, ct);
        await uow.CommitAsync(ct);
        return policy;
    }

    public async Task<SeniorityMovePolicy> GetSeniorityMovePolicyAsync(
        ControlNumber railroadCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(railroadCtrlNbr, craftCtrlNbr)
            ?? throw new KeyNotFoundException($"Seniority move policy for railroad {railroadCtrlNbr} / craft {craftCtrlNbr} not found.");
    }

    public async Task<NoAccessPolicy> GetNoAccessPolicyAsync(
        ControlNumber railroadCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.NoAccessPolicies.GetByRailroadAndCraftAsync(railroadCtrlNbr, craftCtrlNbr)
            ?? throw new KeyNotFoundException($"No access policy for railroad {railroadCtrlNbr} / craft {craftCtrlNbr} not found.");
    }

    public async Task<NoAccessPolicy> GetOrUpsertNoAccessPolicyAsync(
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
        string defaultEffectiveMode,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var railroadCn = ControlNumber.Create(railroadCtrlNbr);
        var craftCn = ControlNumber.Create(craftCtrlNbr);

        if (string.IsNullOrWhiteSpace(defaultEffectiveMode))
            defaultEffectiveMode = NoAccessEffectiveDateMode.NextDay0001;

        var existing = await uow.NoAccessPolicies.GetByRailroadAndCraftAsync(railroadCn, craftCn);
        if (existing is not null)
        {
            existing.Update(
                isEnabled,
                allowEmployeeSelfRequest,
                requireBulletinAccessAudit,
                blockIfOnExtendedAbsence,
                requirePositionCurrentlyAssigned,
                applyExtraBoardSpecialCase,
                requireBoardAvailableForMoveOff,
                autoApproveNoAccess,
                allowAdminOverride,
                blockIfEmployeeMarkedOff,
                blockIfLastVacatedIncumbent,
                defaultEffectiveMode);
            await uow.NoAccessPolicies.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }

        var policy = NoAccessPolicy.Create(
            railroadCn,
            craftCn,
            isEnabled,
            allowEmployeeSelfRequest,
            requireBulletinAccessAudit,
            blockIfOnExtendedAbsence,
            requirePositionCurrentlyAssigned,
            applyExtraBoardSpecialCase,
            requireBoardAvailableForMoveOff,
            autoApproveNoAccess,
            allowAdminOverride,
            blockIfEmployeeMarkedOff,
            blockIfLastVacatedIncumbent,
            defaultEffectiveMode);

        await uow.NoAccessPolicies.AddAsync(policy, ct);
        await uow.CommitAsync(ct);
        return policy;
    }

    public async Task<NoAccessPolicy> CreateMissingNoAccessPolicyAsync(
        long railroadCtrlNbr,
        long craftCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var railroadCn = ControlNumber.Create(railroadCtrlNbr);
        var craftCn = ControlNumber.Create(craftCtrlNbr);

        var existing = await uow.NoAccessPolicies.GetByRailroadAndCraftAsync(railroadCn, craftCn);
        if (existing is not null)
            return existing;

        var policy = NoAccessPolicy.CreateLegacyDefaults(railroadCn, craftCn);
        await uow.NoAccessPolicies.AddAsync(policy, ct);
        await uow.CommitAsync(ct);
        return policy;
    }

    public async Task<SeniorityMove> RequestNoAccessByBulletinAsync(
        long railroadCtrlNbr,
        long craftCtrlNbr,
        long bulletinCtrlNbr,
        long employeeCtrlNbr,
        bool adminOverride,
        CancellationToken ct = default)
    {
        var railroadCn = ControlNumber.Create(railroadCtrlNbr);
        var craftCn = ControlNumber.Create(craftCtrlNbr);
        var employeeCn = ControlNumber.Create(employeeCtrlNbr);
        var bulletinCn = ControlNumber.Create(bulletinCtrlNbr);

        ControlNumber targetPositionCtrlNbr;
        ControlNumber? displacedEmployeeCtrlNbr;
        var targetIsExtraBoard = false;

        await using (var uow = await uowFactory.CreateAsync(cancellationToken: ct))
        {
            var policy = await uow.NoAccessPolicies.GetByRailroadAndCraftAsync(railroadCn, craftCn);
            if (policy is null)
                throw new InvalidOperationException($"No Access policy is not configured for railroad {railroadCn} / craft {craftCn}.");

            if (!policy.IsEnabled)
                throw new InvalidOperationException("No Access is disabled for this craft.");

            if (!policy.AllowEmployeeSelfRequest && !adminOverride)
                throw new InvalidOperationException("Employee self-service No Access requests are disabled for this craft.");

            if (adminOverride && !policy.AllowAdminOverride)
                throw new InvalidOperationException("Admin override is disabled for this craft's No Access policy.");

            var bulletin = await uow.Bulletins.GetByCtrlNbrAsync(bulletinCn, ct)
                ?? throw new KeyNotFoundException($"Bulletin {bulletinCn} not found.");

            if (bulletin.CraftCtrlNbr != craftCn)
                throw new InvalidOperationException("Bulletin craft does not match the selected craft.");

            var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct)
                ?? throw new KeyNotFoundException($"Vacancy {bulletin.PositionVacancyCtrlNbr} for bulletin {bulletinCn} was not found.");

            if (policy.BlockIfLastVacatedIncumbent && vacancy.PreviousIncumbentCtrlNbr == employeeCn)
                throw new InvalidOperationException("Employee is not eligible for No Access on a position they most recently vacated.");

            var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(vacancy.WorkAreaGroupCtrlNbr, ct);
            var vacancyRailroadCn = workArea?.OwningRailroadCtrlNbr ?? workArea?.RailroadCtrlNbr;
            if (vacancyRailroadCn is null || vacancyRailroadCn != railroadCn)
                throw new InvalidOperationException("Bulletin railroad does not match the selected railroad.");

            if (vacancy.TargetType == StaffablePositionType.Crew)
            {
                targetPositionCtrlNbr = vacancy.TargetCtrlNbr;
            }
            else if (vacancy.TargetType == StaffablePositionType.Board)
            {
                var board = await uow.RosterBoards.GetByPositionCtrlNbrAsync(vacancy.TargetCtrlNbr, ct)
                    ?? throw new InvalidOperationException("No Access board bulletin target could not be resolved to a roster board position.");

                var boardPosition = board.Positions.FirstOrDefault(p => p.CtrlNbr == vacancy.TargetCtrlNbr)
                    ?? throw new InvalidOperationException("No Access board bulletin target position was not found on its roster board.");

                targetPositionCtrlNbr = boardPosition.StaffablePositionCtrlNbr;
                targetIsExtraBoard = board.BoardType == BoardType.ExtraBoard;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported bulletin target type '{vacancy.TargetType}' for No Access requests.");
            }

            var employeeAssignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCn);
            var currentAssignment = employeeAssignments
                .OrderByDescending(a => a.AssignedDateUtc)
                .FirstOrDefault();

            var employeeAbsences = await uow.AbsenceRequests.GetByEmployeeAsync(employeeCn);
            var hasActiveMarkOff = employeeAbsences.Any(a =>
                a.Status == "APPROVED"
                && a.EndUtc is null
                && string.Equals(a.ReasonCode, "MARKOFF", StringComparison.OrdinalIgnoreCase));
            if (policy.BlockIfEmployeeMarkedOff && hasActiveMarkOff)
                throw new InvalidOperationException("Employee is currently marked off and is not eligible for No Access requests.");

            if (policy.RequirePositionCurrentlyAssigned && currentAssignment is null)
                throw new InvalidOperationException("Employee is not currently assigned to a position.");

            if (policy.RequireBoardAvailableForMoveOff && currentAssignment is not null)
            {
                var currentBoard = await uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(currentAssignment.StaffablePositionCtrlNbr, ct);
                if (currentBoard is not null && !currentBoard.AllowSeniorityMove)
                    throw new InvalidOperationException($"Current board '{currentBoard.Name}' does not allow moving off by policy.");
            }

            if (policy.ApplyExtraBoardSpecialCase && targetIsExtraBoard)
            {
                var canMoveToExtraBoard = await CanMoveToExtraBoardAsync(uow, employeeCn, craftCn, ct);
                if (!canMoveToExtraBoard)
                    throw new InvalidOperationException("No Access requests to extra-board bulletin targets are blocked unless the employee is eligible to move to the extra board.");
            }

            if (policy.RequireBulletinAccessAudit)
            {
                var hasViewedBulletinDuringWindow = await uow.BulletinAccessAudits.ExistsWithinWindowAsync(
                    bulletin.CtrlNbr,
                    employeeCn,
                    bulletin.BidWindowOpensUtc,
                    bulletin.BidWindowClosesUtc,
                    ct);
                if (hasViewedBulletinDuringWindow)
                    throw new InvalidOperationException("Employee is not eligible for No Access because they viewed this bulletin during its open window.");
            }

            if (policy.BlockIfOnExtendedAbsence)
            {
                var currentBoard = currentAssignment is null
                    ? null
                    : await uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(currentAssignment.StaffablePositionCtrlNbr, ct);
                if (currentBoard is not null && currentBoard.BoardType == BoardType.ExtendedAbsence)
                    throw new InvalidOperationException("No Access requests are blocked while employee is on an Extended Absence board.");
            }

            var targetAssignment = await uow.PositionAssignments.GetByStaffablePositionAsync(targetPositionCtrlNbr);
            displacedEmployeeCtrlNbr = targetAssignment?.EmployeeCtrlNbr;
        }

        var move = await ExerciseSeniorityMoveAsync(
            railroadCtrlNbr,
            employeeCtrlNbr,
            craftCtrlNbr,
            targetPositionCtrlNbr.Value,
            displacedEmployeeCtrlNbr?.Value,
            daysOnCurrentPosition: 0,
            moveType: SeniorityMoveType.NoAccess,
            ct: ct);

        return move;
    }

    private static async Task<bool> CanMoveToExtraBoardAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        ControlNumber craftCtrlNbr,
        CancellationToken ct)
    {
        var assignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
        var currentAssignment = assignments
            .OrderByDescending(a => a.AssignedDateUtc)
            .FirstOrDefault();

        if (currentAssignment is null)
            return false;

        var currentBoard = await uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(currentAssignment.StaffablePositionCtrlNbr, ct);
        if (currentBoard is not null && currentBoard.BoardType == BoardType.Hangout)
            return true;

        if (currentBoard is not null && !currentBoard.AllowSeniorityMove)
            return false;

        var employeeSeniority = await uow.Seniority.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
        var activeSeniority = employeeSeniority.FirstOrDefault(s => s.LastActiveRoster)
            ?? employeeSeniority.OrderByDescending(s => s.RosterDate).FirstOrDefault();
        if (activeSeniority is null)
            return false;

        var craft = await uow.Crafts.GetByCtrlNbrAsync(craftCtrlNbr, ct);
        if (craft?.ParentCtrlNbr is null)
            return false;

        var seniorityStates = await uow.SeniorityStates.GetByParentCtrlNbrAsync(craft.ParentCtrlNbr.Value);
        var activeStateCtrlNbrs = seniorityStates
            .Where(s => s.StateType == StateType.Active)
            .Select(s => s.CtrlNbr)
            .ToHashSet();

        if (!activeStateCtrlNbrs.Contains(activeSeniority.SeniorityStateCtrlNbr))
            return false;

        var boards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(craftCtrlNbr, ct);
        var extraBoardEmployeeCtrlNbrs = boards
            .Where(b => b.RosterCtrlNbr == activeSeniority.RosterCtrlNbr
                        && b.BoardType == BoardType.ExtraBoard
                        && b.AllowSeniorityMove)
            .SelectMany(b => b.Positions)
            .Select(p => p.EmployeeCtrlNbr)
            .Where(cn => cn != employeeCtrlNbr)
            .Distinct()
            .ToList();

        foreach (var candidateCtrlNbr in extraBoardEmployeeCtrlNbrs)
        {
            var candidateSeniority = (await uow.Seniority.GetByEmployeeCtrlNbrAsync(candidateCtrlNbr))
                .FirstOrDefault(s => s.RosterCtrlNbr == activeSeniority.RosterCtrlNbr
                                     && activeStateCtrlNbrs.Contains(s.SeniorityStateCtrlNbr));

            if (candidateSeniority is null)
                continue;

            var isLessSenior = candidateSeniority.RosterDate > activeSeniority.RosterDate
                || (candidateSeniority.RosterDate == activeSeniority.RosterDate
                    && candidateSeniority.Rank > activeSeniority.Rank);

            if (isLessSenior)
                return true;
        }

        return false;
    }

    public async Task<IReadOnlyList<(Craft Craft, NoAccessPolicy? Policy)>> ListNoAccessPoliciesByRailroadAsync(
        ControlNumber railroadCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var allRosters = await uow.Rosters.GetAllAsync(ct);
        var workAreaIds = allRosters
            .Select(r => r.WorkAreaGroupCtrlNbr)
            .Distinct()
            .ToList();
        var workAreas = await uow.DynamicGroups.GetByCtrlNbrsAsync(workAreaIds);
        var workAreaById = workAreas.ToDictionary(g => g.CtrlNbr, g => g);

        var craftIdsForRailroad = allRosters
            .Where(r => workAreaById.TryGetValue(r.WorkAreaGroupCtrlNbr, out var wa)
                        && wa.OwningRailroadCtrlNbr == railroadCtrlNbr)
            .Select(r => r.CraftCtrlNbr)
            .Distinct()
            .ToHashSet();

        if (craftIdsForRailroad.Count == 0)
            return [];

        var crafts = (await uow.Crafts.GetByCtrlNbrsAsync(craftIdsForRailroad))
            .OrderBy(c => c.CraftNumber)
            .ThenBy(c => c.CraftName)
            .ToList();

        var policies = await uow.NoAccessPolicies.GetByRailroadAsync(railroadCtrlNbr);
        var policyByCraft = policies.ToDictionary(p => p.CraftCtrlNbr, p => p);

        return crafts
            .Select(c => (c, policyByCraft.GetValueOrDefault(c.CtrlNbr)))
            .ToList();
    }

    public async Task<SeniorityMove> ExerciseSeniorityMoveAsync(
        long railroadCtrlNbr, long employeeCtrlNbr, long craftCtrlNbr, long targetPositionCtrlNbr,
        long? displacedEmployeeCtrlNbr, int daysOnCurrentPosition,
        string moveType = SeniorityMoveType.Voluntary,
        long targetBoardCtrlNbr = 0,
        bool? willWork = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var empCtrlNbr = ControlNumber.Create(employeeCtrlNbr);
        var policy = await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(ControlNumber.Create(railroadCtrlNbr), ControlNumber.Create(craftCtrlNbr));

        // Compute days on current position server-side from the live PositionAssignment.
        var currentAssignments = await uow.PositionAssignments.GetByEmployeeAsync(empCtrlNbr);
        var latestAssignment = currentAssignments
            .OrderByDescending(a => a.AssignedDateUtc)
            .FirstOrDefault();
        if (latestAssignment is not null)
            daysOnCurrentPosition = (int)(workAreaClock.UtcNow.UtcDateTime - latestAssignment.AssignedDateUtc).TotalDays;

        var currentBoardType = await ResolveCurrentBoardTypeAsync(
            uow, ControlNumber.Create(craftCtrlNbr), empCtrlNbr, latestAssignment, ct);
        var strategy = ResolveMoveStrategy(policy, currentBoardType, targetBoardCtrlNbr);
        var requiredEligibilityDays = ResolveRequiredEligibilityDays(policy, currentBoardType, targetBoardCtrlNbr);

        var noAccessAutoApprove = moveType == SeniorityMoveType.NoAccess
            ? (await uow.NoAccessPolicies.GetByRailroadAndCraftAsync(
                ControlNumber.Create(railroadCtrlNbr),
                ControlNumber.Create(craftCtrlNbr)))?.AutoApproveNoAccess
            : null;

        // No Access is an administrative forced bump that bypasses the eligibility threshold.
        var isNoAccess = moveType == SeniorityMoveType.NoAccess;

        if (!isNoAccess && daysOnCurrentPosition < requiredEligibilityDays)
            throw new InvalidOperationException(
                $"Employee has only {daysOnCurrentPosition} days on current position; eligibility requires {requiredEligibilityDays}.");

        // Compute effective date. No Access uses a fixed next-day floor (legacy SA rule:
        // DateTime.Today.AddDays(1).AddMinutes(1)); all other moves use the policy-driven strategy.
        var effectiveUtc = isNoAccess
            ? workAreaClock.UtcNow.UtcDateTime.Date.AddDays(1).AddMinutes(1)
            : (await ComputeEffectiveDateAsync(
                empCtrlNbr, ControlNumber.Create(craftCtrlNbr),
                targetBoardCtrlNbr, targetPositionCtrlNbr, latestAssignment, daysOnCurrentPosition,
                policy, uow, ct)).UtcDateTime;

        // Board join path: create a new position at the bottom of the target board.
        if (targetBoardCtrlNbr > 0)
        {
            var board = await uow.RosterBoards.GetByCtrlNbrAsync(ControlNumber.Create(targetBoardCtrlNbr), ct)
                ?? throw new KeyNotFoundException($"Roster board {targetBoardCtrlNbr} not found.");

            if (!board.AllowSeniorityMove)
                throw new InvalidOperationException($"Board '{board.Name}' does not allow seniority moves.");

            var nextOrder = board.Positions.Count > 0
                ? board.Positions.Max(p => p.PositionOrder) + 1
                : 1;

            var staffablePosition = StaffablePosition.Create(StaffablePositionType.Board);
            var boardPosition = board.AddPosition(empCtrlNbr, nextOrder, staffablePosition.CtrlNbr);
            var positionAssignment = PositionAssignment.Create(
                staffablePosition.CtrlNbr, empCtrlNbr, PositionAssignmentType.Board, boardPosition.CtrlNbr);

            uow.StaffablePositions.Add(staffablePosition);
            uow.PositionAssignments.Add(positionAssignment);
            uow.RosterBoards.Update(board);

            targetPositionCtrlNbr = staffablePosition.CtrlNbr.Value;
            displacedEmployeeCtrlNbr = null;
        }

        // Bump path: if targetPositionCtrlNbr was not supplied, resolve it from the displaced employee's current assignment.
        if (targetPositionCtrlNbr == 0 && displacedEmployeeCtrlNbr is > 0)
        {
            var displacedAssignments = await uow.PositionAssignments.GetByEmployeeAsync(ControlNumber.Create(displacedEmployeeCtrlNbr.Value));
            var displacedAssignment = displacedAssignments.FirstOrDefault()
                ?? throw new InvalidOperationException($"Displaced employee {displacedEmployeeCtrlNbr} has no current position assignment.");
            targetPositionCtrlNbr = displacedAssignment.StaffablePositionCtrlNbr.Value;
        }

        if (targetPositionCtrlNbr == 0)
            throw new InvalidOperationException("A target position or target board must be specified for a seniority move.");

        // Will-work election is only honored when the governing policy enables it.
        // Otherwise no election is recorded (null), matching the legacy "option not offered" case.
        var willWorkElection = policy?.WillWorkEnabled == true ? willWork : null;

        var move = SeniorityMove.Create(ControlNumber.Create(railroadCtrlNbr), ControlNumber.Create(employeeCtrlNbr), ControlNumber.Create(craftCtrlNbr),
            ControlNumber.Create(targetPositionCtrlNbr),
            displacedEmployeeCtrlNbr is null or 0 ? null : ControlNumber.Create(displacedEmployeeCtrlNbr.Value),
            daysOnCurrentPosition, moveType, effectiveUtc, willWorkElection);
        await uow.SeniorityMoves.AddAsync(move, ct);

        // Notify the soon-to-be-displaced employee at request time (position-affecting; requires
        // acknowledgement). Mirrors the legacy SeniorityMoveNotification raised on creation.
        await notifications.NotifySeniorityMoveRequestedAsync(uow, move, ct);

        await uow.CommitAsync(ct);
        seniorityMoveSignal.Notify(move.EffectiveUtc ?? workAreaClock.UtcNow.UtcDateTime);

        var autoApprove = move.MoveType == SeniorityMoveType.Hangout
            || (move.MoveType == SeniorityMoveType.NoAccess
                ? noAccessAutoApprove != false
                : policy?.AutoApprove == true);
        var dueNow = move.EffectiveUtc.HasValue && move.EffectiveUtc.Value <= workAreaClock.UtcNow.UtcDateTime;

        if (autoApprove && dueNow)
        {
            await ApproveSeniorityMoveAsync(move.CtrlNbr, ct: ct);
            await seniorityMoveExecutionService.ExecuteAsync(move.CtrlNbr, ct);

            await using var refreshUow = await uowFactory.CreateAsync(cancellationToken: ct);
            return await refreshUow.SeniorityMoves.GetByCtrlNbrAsync(move.CtrlNbr, ct) ?? move;
        }

        return move;
    }

    public async Task<SeniorityMove> ApproveSeniorityMoveAsync(
        ControlNumber moveCtrlNbr, DateTime? effectiveUtc = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var move = await uow.SeniorityMoves.GetByCtrlNbrAsync(moveCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority move {moveCtrlNbr} not found.");
        move.Approve(effectiveUtc);
        await uow.SeniorityMoves.UpdateAsync(move, ct);
        await uow.CommitAsync(ct);
        // Wake the worker at the move's effective time (or immediately if none set)
        seniorityMoveSignal.Notify(move.EffectiveUtc ?? workAreaClock.UtcNow.UtcDateTime);
        return move;
    }

    public async Task<SeniorityMove> RejectSeniorityMoveAsync(
        ControlNumber moveCtrlNbr, string reason, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var move = await uow.SeniorityMoves.GetByCtrlNbrAsync(moveCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority move {moveCtrlNbr} not found.");
        move.Reject(reason);
        await uow.SeniorityMoves.UpdateAsync(move, ct);
        await uow.CommitAsync(ct);
        return move;
    }

    public async Task<SeniorityMove> CancelSeniorityMoveAsync(
        ControlNumber moveCtrlNbr, string reason, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var move = await uow.SeniorityMoves.GetByCtrlNbrAsync(moveCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority move {moveCtrlNbr} not found.");

        if (move.MoveType == SeniorityMoveType.Hangout)
        {
            var isAdmin = currentUserService.IsInRole(Roles.SystemAdmin)
                || currentUserService.IsInRole(Roles.ParentAdmin)
                || currentUserService.IsInRole(Roles.RailroadAdmin);

            if (!isAdmin)
                throw new InvalidOperationException("Only admins can cancel Hangout auto-moves.");
        }

        // Enforce CancelHours: cannot cancel if within the cancel window before effective time.
        if (move.Status == SeniorityMoveStatus.Approved && move.EffectiveUtc.HasValue)
        {
            var policy = await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(move.RailroadCtrlNbr, move.CraftCtrlNbr);
            if (policy is not null && policy.CancelHours > 0)
            {
                var cancelDeadline = move.EffectiveUtc.Value.AddHours(-policy.CancelHours);
                if (workAreaClock.UtcNow.UtcDateTime > cancelDeadline)
                    throw new InvalidOperationException(
                        $"Cannot cancel: within the {policy.CancelHours}-hour cancel window before effective time {move.EffectiveUtc.Value:u}.");
            }
        }

        move.Cancel(reason);
        await uow.SeniorityMoves.UpdateAsync(move, ct);

        // Notify the previously-bumped employee that the move is off, and clear the stale bump notice.
        await notifications.NotifySeniorityMoveCancelledAsync(uow, move, ct);

        await uow.CommitAsync(ct);
        return move;
    }

    public async Task<SeniorityMove> CompleteSeniorityMoveAsync(
        ControlNumber moveCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var move = await uow.SeniorityMoves.GetByCtrlNbrAsync(moveCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority move {moveCtrlNbr} not found.");
        move.Complete();
        await uow.SeniorityMoves.UpdateAsync(move, ct);
        await uow.CommitAsync(ct);
        return move;
    }

    public async Task<IReadOnlyList<SeniorityMoveListItem>> GetSeniorityMovesByEmployeeAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var moves = await uow.SeniorityMoves.GetByEmployeeAsync(employeeCtrlNbr, ct);
        return await EnrichWithAutoApproveAsync(moves, uow, ct);
    }

    public async Task<IReadOnlyList<SeniorityMoveListItem>> GetSeniorityMovesByCraftAsync(
        ControlNumber craftCtrlNbr, string? status = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var moves = status is not null
            ? await uow.SeniorityMoves.GetByCraftByStatusAsync(craftCtrlNbr, status, ct)
            : await uow.SeniorityMoves.GetByCraftAsync(craftCtrlNbr, ct);
        return await EnrichWithAutoApproveAsync(moves, uow, ct);
    }

    public async Task<IReadOnlyList<SeniorityMoveListItem>> GetPendingSeniorityMovesAsync(
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var moves = await uow.SeniorityMoves.GetPendingAsync(ct);
        return await EnrichWithAutoApproveAsync(moves, uow, ct);
    }

    public async Task<IReadOnlyList<SeniorityMoveListItem>> GetActiveSeniorityMovesAsync(
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var moves = await uow.SeniorityMoves.GetActiveAsync(ct);
        return await EnrichWithAutoApproveAsync(moves, uow, ct);
    }

    public async Task<IReadOnlyList<SeniorityMoveListItem>> GetAllSeniorityMovesAsync(
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var moves = await uow.SeniorityMoves.GetAllMovesAsync(ct);
        return await EnrichWithAutoApproveAsync(moves, uow, ct);
    }

    /// <summary>
    /// Pairs each move with its computed auto-approve flag, mirroring the
    /// <c>SeniorityMoveWorker</c> predicate: a move auto-approves when it is a
    /// NoAccess bump, or when its craft policy exists and has AutoApprove enabled.
    /// Policies are looked up once per craft. Each move is also paired with its
    /// resolved target-position display name and the work-area timezone id of the
    /// target position, both cached once per position.
    /// </summary>
    private async Task<IReadOnlyList<SeniorityMoveListItem>> EnrichWithAutoApproveAsync(
        List<SeniorityMove> moves, IOrchestrationUnitOfWork uow, CancellationToken ct)
    {
        var policyCache = new Dictionary<ControlNumber, SeniorityMovePolicy?>();
        var targetNameCache = new Dictionary<ControlNumber, string>();
        var timeZoneIdCache = new Dictionary<ControlNumber, string?>();
        var items = new List<SeniorityMoveListItem>(moves.Count);
        foreach (var move in moves)
        {
            bool autoApprove;
            if (move.MoveType == SeniorityMoveType.NoAccess)
            {
                autoApprove = true;
            }
            else
            {
                if (!policyCache.TryGetValue(move.CraftCtrlNbr, out var policy))
                {
                    policy = await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(move.RailroadCtrlNbr, move.CraftCtrlNbr);
                    policyCache[move.CraftCtrlNbr] = policy;
                }
                autoApprove = policy is not null && policy.AutoApprove;
            }

            if (!targetNameCache.TryGetValue(move.TargetPositionCtrlNbr, out var targetName))
            {
                targetName = await StaffablePositionNameResolver.ResolveAsync(uow, move.TargetPositionCtrlNbr, ct);
                targetNameCache[move.TargetPositionCtrlNbr] = targetName;
            }

            // Resolve the work-area timezone of the target position so the UI can
            // display the move's UTC instants as work-area-local wall-clock times.
            // Crew targets resolve via Crew -> WorkArea. Board targets resolve via
            // Board -> Roster -> WorkArea.
            if (!timeZoneIdCache.TryGetValue(move.TargetPositionCtrlNbr, out var timeZoneId))
            {
                var crewPos = await uow.CrewPositions.GetByStaffablePositionAsync(move.TargetPositionCtrlNbr);

                if (crewPos is not null)
                {
                    var tz = await workAreaClock.GetCrewTimeZoneAsync(uow, crewPos.CrewCtrlNbr, ct);
                    timeZoneId = tz?.Id;
                }
                else
                {
                    var board = await uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(move.TargetPositionCtrlNbr, ct);
                    if (board is not null)
                    {
                        var roster = await uow.Rosters.GetByCtrlNbrAsync(board.RosterCtrlNbr, ct);
                        var workArea = roster is null
                            ? null
                            : await uow.DynamicGroups.GetByCtrlNbrAsync(roster.WorkAreaGroupCtrlNbr, ct);
                        timeZoneId = workArea?.TimeZoneId;
                    }
                }

                timeZoneIdCache[move.TargetPositionCtrlNbr] = timeZoneId;
            }

            items.Add(new SeniorityMoveListItem(move, autoApprove, targetName, timeZoneId));
        }
        return items;
    }

    public async Task<IReadOnlyList<SeniorityMove>> GetApprovedDueSeniorityMovesAsync(
        DateTime asOf, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.SeniorityMoves.GetApprovedDueAsync(asOf, ct);
    }

    public async Task<DateTime?> GetNextApprovedSeniorityMoveEffectiveUtcAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.SeniorityMoves.GetNextApprovedEffectiveUtcAsync(ct);
    }

    public async Task<DateTime?> GetNextActiveSeniorityMoveEffectiveUtcForRailroadAsync(
        ControlNumber railroadCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var active = await uow.SeniorityMoves.GetActiveAsync(ct);
        var nowUtc = workAreaClock.UtcNow.UtcDateTime;
        return active
            .Where(m => m.RailroadCtrlNbr == railroadCtrlNbr
                        && m.EffectiveUtc.HasValue
                        && m.EffectiveUtc.Value >= nowUtc)
            .OrderBy(m => m.EffectiveUtc)
            .Select(m => (DateTime?)m.EffectiveUtc!.Value)
            .FirstOrDefault();
    }

    /// <summary>
    /// Auto-approves all Pending seniority moves whose craft policy has <c>AutoApprove = true</c>.
    /// Called by <c>SeniorityMoveWorker</c>.
    /// </summary>
    public async Task<IReadOnlyList<SeniorityMove>> AutoApprovePendingMovesAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var pending = await uow.SeniorityMoves.GetPendingAsync(ct);
        var approved = new List<SeniorityMove>();

        foreach (var move in pending)
        {
            // No Access and Hangout are system-driven moves: they always auto-approve,
            // regardless of whether a policy exists or has AutoApprove enabled.
            if (move.MoveType != SeniorityMoveType.NoAccess && move.MoveType != SeniorityMoveType.Hangout)
            {
                var policy = await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(move.RailroadCtrlNbr, move.CraftCtrlNbr);
                if (policy is null || !policy.AutoApprove) continue;
            }
            move.Approve();
            await uow.SeniorityMoves.UpdateAsync(move, ct);
            approved.Add(move);
        }

        if (approved.Count > 0)
        {
            await uow.CommitAsync(ct);
            // Notify the signal for the earliest approved effective time
            var earliest = approved
                .Where(m => m.EffectiveUtc.HasValue)
                .OrderBy(m => m.EffectiveUtc)
                .FirstOrDefault();
            if (earliest?.EffectiveUtc is not null)
                seniorityMoveSignal.Notify(earliest.EffectiveUtc.Value);
            else if (approved.Count > 0)
                seniorityMoveSignal.Notify(workAreaClock.UtcNow.UtcDateTime);
        }
        return approved;
    }

    /// <summary>
    /// Returns the computed effective date for a prospective seniority move without persisting anything.
    /// Used by the UI to display the effective date to the employee before they submit.
    /// </summary>
    public async Task<DateTimeOffset> PreviewEffectiveDateAsync(
        long railroadCtrlNbr, long employeeCtrlNbr, long craftCtrlNbr,
        long targetPositionCtrlNbr = 0, long targetBoardCtrlNbr = 0,
        CancellationToken ct = default)
    {
        var (effectiveUtc, _) = await PreviewEffectiveDateWithWillWorkAsync(
            railroadCtrlNbr, employeeCtrlNbr, craftCtrlNbr, targetPositionCtrlNbr, targetBoardCtrlNbr, ct);
        return effectiveUtc;
    }

    /// <summary>
    /// Computes the effective date and whether the "will work" election should be offered.
    /// The election is offered only when the governing policy enables it, the employee is on a
    /// crew position (not a board), and the effective time-of-day equals the current crew
    /// position's on-duty time (i.e. the move takes effect at the start of a shift they would
    /// otherwise work). Mirrors SA's <c>SeniorityMove.WillWorkOption</c>.
    /// </summary>
    public async Task<(DateTimeOffset EffectiveUtc, bool WillWorkOffered)> PreviewEffectiveDateWithWillWorkAsync(
        long railroadCtrlNbr, long employeeCtrlNbr, long craftCtrlNbr,
        long targetPositionCtrlNbr = 0, long targetBoardCtrlNbr = 0,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var empCtrlNbr = ControlNumber.Create(employeeCtrlNbr);
        var policy = await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(ControlNumber.Create(railroadCtrlNbr), ControlNumber.Create(craftCtrlNbr));

        var currentAssignments = await uow.PositionAssignments.GetByEmployeeAsync(empCtrlNbr);
        var latestAssignment = currentAssignments.OrderByDescending(a => a.AssignedDateUtc).FirstOrDefault();

        int daysOnCurrentPosition = latestAssignment is not null
            ? (int)(workAreaClock.UtcNow.UtcDateTime - latestAssignment.AssignedDateUtc).TotalDays
            : 0;

        var effectiveUtc = await ComputeEffectiveDateAsync(
            empCtrlNbr, ControlNumber.Create(craftCtrlNbr),
            targetBoardCtrlNbr, targetPositionCtrlNbr, latestAssignment, daysOnCurrentPosition,
            policy, uow, ct);

        var willWorkOffered = await IsWillWorkOfferedAsync(
            policy, latestAssignment, effectiveUtc, uow, ct);

        return (effectiveUtc, willWorkOffered);
    }

    /// <summary>
    /// Determines whether the "will work" election is offered for a move with the given effective date.
    /// Legacy rule (SA <c>WillWorkOption</c>): the employee is on a crew position and the effective
    /// time-of-day equals that crew position's on-duty time.
    /// </summary>
    private async Task<bool> IsWillWorkOfferedAsync(
        SeniorityMovePolicy? policy,
        PositionAssignment? currentAssignment,
        DateTimeOffset effectiveUtc,
        IOrchestrationUnitOfWork uow,
        CancellationToken ct)
    {
        if (policy?.WillWorkEnabled != true) return false;
        if (currentAssignment is null) return false;
        // Only crew positions qualify; board members are never offered the election.
        if (currentAssignment.AssignmentType != PositionAssignmentType.Direct ||
            currentAssignment.AssignmentSourceCtrlNbr is null)
            return false;

        var currentCrewPos = await uow.CrewPositions.GetByCtrlNbrAsync(currentAssignment.AssignmentSourceCtrlNbr, ct);
        var (schedule, _) = await ResolveCrewScheduleAsync(currentCrewPos, uow, ct);
        if (schedule is null || currentCrewPos is null) return false;

        // The on-duty time is a work-area-local wall clock; compare it against the effective
        // instant converted into that same zone.
        var tz = await workAreaClock.GetCrewTimeZoneAsync(uow, currentCrewPos.CrewCtrlNbr, ct);
        var localEffective = tz is null
            ? effectiveUtc.UtcDateTime
            : TimeZoneInfo.ConvertTimeFromUtc(effectiveUtc.UtcDateTime, tz);

        return TimeOnly.FromDateTime(localEffective) == schedule.OnDutyTime;
    }

    /// <summary>
    /// Computes the seniority move effective date using policy-driven strategy fields
    /// and legacy SA rules ported to the new schedule model.
    ///
    /// Strategy dispatch (read from SeniorityMovePolicy):
    ///   Immediate        – effective = UtcNow
    ///   RequestLeadTime  – effective = max(UtcNow + RequestHours, BumpDate)  [no schedule]
    ///   FirstOffDay      – end-of-shift on the last work day of the relevant schedule period;
    ///                      rolls +7 days when within RequestHours lead-time window.
    ///                      Board path: uses CURRENT crew schedule (Engineer end-of-week).
    ///                      Crew path:  uses TARGET position's schedule.
    /// </summary>
    private async Task<DateTimeOffset> ComputeEffectiveDateAsync(
        ControlNumber empCtrlNbr,
        ControlNumber craftCtrlNbr,
        long targetBoardCtrlNbr,
        long targetPositionCtrlNbr,
        PositionAssignment? currentAssignment,
        int daysOnCurrentPosition,
        SeniorityMovePolicy? policy,
        IOrchestrationUnitOfWork uow,
        CancellationToken ct)
    {
        // Instant arithmetic below is in UTC. Schedule-derived wall-clock times (FirstOffDay)
        // are interpreted in the relevant work area's timezone and converted to a true UTC
        // instant before returning.
        static DateTimeOffset AsUtc(DateTime dt) => new(DateTime.SpecifyKind(dt, DateTimeKind.Utc));

        var now          = workAreaClock.UtcNow.UtcDateTime;
        int requestHours = policy?.RequestHours   ?? 0;

        var currentBoardType = await ResolveCurrentBoardTypeAsync(uow, craftCtrlNbr, empCtrlNbr, currentAssignment, ct);
        var requiredEligibilityDays = ResolveRequiredEligibilityDays(policy, currentBoardType, targetBoardCtrlNbr);

        // BumpDate: earliest date the employee became/becomes eligible for this transition.
        var bumpDate = currentAssignment is not null
            ? currentAssignment.AssignedDateUtc.AddDays(requiredEligibilityDays)
            : now;

        // Resolve the strategy for this transition.
        var strategy = ResolveMoveStrategy(policy, currentBoardType, targetBoardCtrlNbr);

        if (string.IsNullOrEmpty(strategy))
        {
            var sourceLabel = currentBoardType switch
            {
                BoardType.ExtraBoard => "Extra Board",
                BoardType.Hangout => "Hangout",
                BoardType.ExtendedAbsence => "Extended Absence",
                BoardType.Training => "Training",
                BoardType.NewHire => "New Hire",
                null => "Crew",
                _ => "Current Source"
            };

            var targetLabel = targetBoardCtrlNbr > 0 ? "Board" : "Crew Position";

            throw new InvalidOperationException(
                $"Cannot move from {sourceLabel} to {targetLabel}.");
        }

        // ── Immediate ──────────────────────────────────────────────────────────
        if (strategy == SeniorityMoveEffectiveDateStrategy.Immediate)
            return AsUtc(now);

        // ── RequestLeadTime ────────────────────────────────────────────────────
        if (strategy == SeniorityMoveEffectiveDateStrategy.RequestLeadTime)
        {
            var baseDate = now.AddHours(requestHours);
            if (bumpDate > baseDate) baseDate = bumpDate;
            // Yardman/Yardmaster: avoid exact midnight (legacy nudge rule).
            var craft     = await uow.Crafts.GetByCtrlNbrAsync(craftCtrlNbr, ct);
            var craftName = craft?.CraftName ?? string.Empty;
            if ((craftName.Contains("Yardman", StringComparison.OrdinalIgnoreCase) ||
                 craftName.Contains("Yardmaster", StringComparison.OrdinalIgnoreCase))
                && baseDate.TimeOfDay == TimeSpan.Zero)
                baseDate = baseDate.AddMinutes(1);
            return AsUtc(baseDate);
        }

        // ── FirstOffDay ────────────────────────────────────────────────────────
        // Resolve the relevant schedule and the crew position it belongs to (used to resolve
        // the work-area timezone for the off-duty wall-clock time).
        AssignmentSchedule? schedule = null;
        CrewPosition? scheduleCrewPos = null;
        int workDaysMask = 0;

        if (targetBoardCtrlNbr > 0)
        {
            // Moving to a board: use the CURRENT crew position's schedule.
            // (Engineer Crew→Board uses current crew end-of-work-week.)
            if (currentAssignment?.AssignmentType == PositionAssignmentType.Direct &&
                currentAssignment.AssignmentSourceCtrlNbr is not null)
            {
                scheduleCrewPos = await uow.CrewPositions.GetByCtrlNbrAsync(
                    currentAssignment.AssignmentSourceCtrlNbr, ct);
                (schedule, workDaysMask) = await ResolveCrewScheduleAsync(scheduleCrewPos, uow, ct);
            }
        }
        else
        {
            // Moving to a crew position: always use the TARGET position's schedule.
            if (targetPositionCtrlNbr > 0)
            {
                scheduleCrewPos = await uow.CrewPositions.GetByStaffablePositionAsync(
                    ControlNumber.Create(targetPositionCtrlNbr));
                (schedule, workDaysMask) = await ResolveCrewScheduleAsync(scheduleCrewPos, uow, ct);
            }
        }

        // Timezone of the work area that owns the resolved schedule. Null = treat as UTC.
        var scheduleTz = scheduleCrewPos is not null
            ? await workAreaClock.GetCrewTimeZoneAsync(uow, scheduleCrewPos.CrewCtrlNbr, ct)
            : null;

        if (targetBoardCtrlNbr > 0)
        {
            // Board move (FirstOffDay): end of current crew's last work day this week.
            var baseDate = now.AddHours(requestHours);
            if (bumpDate > baseDate) baseDate = bumpDate;

            if (schedule is not null)
                return GetNextEndOfWorkWeek(schedule, AsUtc(baseDate), requestHours, scheduleTz, workDaysMask);

            // No schedule: fall back to next Monday (legacy fallback for Engineers).
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)baseDate.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7;
            return AsUtc(baseDate.Date.AddDays(daysUntilMonday));
        }
        else
        {
            // Crew bump (FirstOffDay): last work day of target schedule.
            var anchor = daysOnCurrentPosition < requiredEligibilityDays ? bumpDate : now;

            if (schedule is not null)
                return GetNextEndOfWorkWeek(schedule, AsUtc(anchor), requestHours, scheduleTz, workDaysMask);

            // No schedule: fall back to anchor + RequestHours.
            return AsUtc(anchor.AddHours(requestHours));
        }
    }

    /// <summary>
    /// Resolves the AssignmentSchedule and work-days mask that govern a crew's end-of-work-week.
    /// A crew's true work week is the UNION of all its crew assignments' day masks: a regular crew
    /// has a single assignment, while a relief crew covers several assignments on different days
    /// (e.g. RLF-A: Sun on one, Mon/Tue on another, Wed/Thu on a third → Sun–Thu). The end-of-week
    /// day is the latest day in that union, and the governing off-duty time comes from the
    /// assignment that covers that last day. Returns null when the crew position has no schedule.
    /// </summary>
    private static async Task<(AssignmentSchedule? Schedule, int WorkDaysMask)> ResolveCrewScheduleAsync(
        CrewPosition? crewPosition,
        IOrchestrationUnitOfWork uow,
        CancellationToken ct)
    {
        _ = ct;
        if (crewPosition is null) return (null, 0);

        var crewAssignments = await uow.CrewAssignments.GetByCrewAsync(crewPosition.CrewCtrlNbr);
        if (crewAssignments.Count == 0) return (null, 0);

        // Union of every assignment's days = the crew's actual weekly footprint.
        int unionMask = 0;
        foreach (var ca in crewAssignments) unionMask |= ca.DaysOfWeekMask;

        // Last day of the crew's contiguous work block. Uses the rest-gap rule so weeks that wrap
        // the Sat->Sun boundary resolve correctly (e.g. Fri,Sat,Sun,Mon,Tue ends on Tuesday).
        int lastDay = FindLastWorkDayOfWeek(unionMask);

        // The assignment covering that last day supplies the governing schedule (its off-duty
        // wall-clock time is when the crew finishes for the week). Fall back to the assignment with
        // the most days when no union day is set (defensive; shouldn't happen with real data).
        var governingAssignment = lastDay >= 0
            ? crewAssignments.First(ca => (ca.DaysOfWeekMask & (1 << lastDay)) != 0)
            : crewAssignments.OrderByDescending(ca => CountBits(ca.DaysOfWeekMask)).First();

        var schedules = await uow.AssignmentSchedules
            .GetByAssignmentAsync(governingAssignment.AssignmentCtrlNbr);

        // Among that assignment's schedules, pick the shift whose operating days best overlap the
        // crew's days on that assignment (handles multi-shift assignments).
        var schedule = schedules
            .OrderByDescending(s => CountBits(s.OperatingDaysMask & governingAssignment.DaysOfWeekMask))
            .FirstOrDefault();

        return (schedule, governingAssignment.DaysOfWeekMask);
    }

    private static string ResolveMoveStrategy(
        SeniorityMovePolicy? policy,
        BoardType? currentBoardType,
        long targetBoardCtrlNbr)
    {
        if (targetBoardCtrlNbr > 0)
            return policy?.CrewToBoardStrategy ?? string.Empty;

        return currentBoardType switch
        {
            BoardType.ExtraBoard => policy?.ExtraBoardToCrewStrategy ?? string.Empty,
            BoardType.Hangout => policy?.HangoutToCrewStrategy ?? string.Empty,
            BoardType.ExtendedAbsence => policy?.ExtendedAbsenceToCrewStrategy ?? string.Empty,
            BoardType.Training => policy?.TrainingToCrewStrategy ?? string.Empty,
            BoardType.NewHire => policy?.NewHireToCrewStrategy ?? string.Empty,
            null => policy?.CrewToCrewStrategy ?? string.Empty,
            _ => string.Empty
        };
    }

    private static int ResolveRequiredEligibilityDays(
        SeniorityMovePolicy? policy,
        BoardType? currentBoardType,
        long targetBoardCtrlNbr)
    {
        if (policy is null)
            return 0;

        if (targetBoardCtrlNbr > 0)
            return policy.CrewToBoardEligibilityDays;

        return currentBoardType switch
        {
            BoardType.ExtraBoard => policy.ExtraBoardToCrewEligibilityDays,
            BoardType.Hangout => policy.HangoutToCrewEligibilityDays,
            BoardType.ExtendedAbsence => policy.ExtendedAbsenceToCrewEligibilityDays,
            BoardType.Training => policy.TrainingToCrewEligibilityDays,
            BoardType.NewHire => policy.NewHireToCrewEligibilityDays,
            null => policy.CrewToCrewEligibilityDays,
            _ => 0
        };
    }

    private static async Task<BoardType?> ResolveCurrentBoardTypeAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber craftCtrlNbr,
        ControlNumber empCtrlNbr,
        PositionAssignment? currentAssignment,
        CancellationToken ct)
    {
        if (currentAssignment is null)
            return null;

        var boards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(craftCtrlNbr, ct);
        var boardPosition = boards
            .SelectMany(b => b.Positions)
            .FirstOrDefault(p =>
                p.StaffablePositionCtrlNbr == currentAssignment.StaffablePositionCtrlNbr &&
                p.EmployeeCtrlNbr == empCtrlNbr);

        if (boardPosition is null)
            return null;

        var currentBoard = boards.FirstOrDefault(b => b.CtrlNbr == boardPosition.RosterBoardCtrlNbr);
        return currentBoard?.BoardType;
    }

    /// <summary>
    /// Returns the off-duty time of the LAST scheduled work day of the current schedule week
    /// whose off-duty datetime is after <paramref name="baseDate"/>. If that time is before
    /// the RequestHours minimum lead time, advances by 7 days (legacy SA rule).
    /// </summary>
    private DateTimeOffset GetNextEndOfWorkWeek(
        AssignmentSchedule schedule, DateTimeOffset baseDate, int requestHours, TimeZoneInfo? tz, int workDaysMask)
    {
        var minTime = baseDate.AddHours(requestHours);

        // Find the LAST day the crew actually works: the crew's work days narrowed to the
        // schedule's operating days. The assignment may be staffed every day while the crew only
        // covers part of the week (e.g. a relief crew on Wed/Thu), so the crew mask — not the full
        // schedule mask — determines the end-of-work-week day. Fall back to the schedule's
        // operating days when no crew mask is available or the intersection is empty.
        int effectiveMask = workDaysMask != 0 ? schedule.OperatingDaysMask & workDaysMask : schedule.OperatingDaysMask;
        if (effectiveMask == 0) effectiveMask = schedule.OperatingDaysMask;

        // Last day of the contiguous work block (rest-gap rule), so weeks that wrap the Sat->Sun
        // boundary (e.g. Fri,Sat,Sun,Mon,Tue) correctly end on Tuesday rather than Saturday.
        int last = FindLastWorkDayOfWeek(effectiveMask);
        DayOfWeek? lastWorkDay = last >= 0 ? (DayOfWeek)last : null;

        if (lastWorkDay is null) return baseDate;

        // Walk the day-of-week in WORK-AREA-LOCAL time and combine with the local off-duty
        // wall-clock time, then convert to a true UTC instant. Combining a UTC date with a
        // local TimeOnly (the old behavior) produced an instant wrong by the zone offset,
        // which is what shifted displayed effective times (e.g. 7:00 AM → 2:00 AM).
        var localBase = tz is null
            ? baseDate.UtcDateTime
            : TimeZoneInfo.ConvertTimeFromUtc(baseDate.UtcDateTime, tz);

        int daysToAdd = ((int)lastWorkDay.Value - (int)localBase.DayOfWeek + 7) % 7;
        var localDate = DateOnly.FromDateTime(localBase).AddDays(daysToAdd);
        var endOfWeek = workAreaClock.CombineLocalToUtc(localDate, schedule.OffDutyTime, tz);

        if (endOfWeek < minTime)
            endOfWeek = workAreaClock.CombineLocalToUtc(localDate.AddDays(7), schedule.OffDutyTime, tz);

        return endOfWeek;
    }
    /// <summary>Counts the number of set bits (population count) in a bitmask.</summary>
    private static int CountBits(int mask)
    {
        int count = 0;
        while (mask != 0) { count += mask & 1; mask >>= 1; }
        return count;
    }

    /// <summary>
    /// Returns the last day (0=Sun .. 6=Sat) of the crew's primary contiguous work block using the
    /// rest-gap rule: the last work day is one whose following day is a rest day. This resolves
    /// schedules that wrap the Sat->Sun boundary correctly — e.g. Fri,Sat,Sun,Mon,Tue (off Wed,Thu)
    /// ends on Tuesday, not Saturday. When multiple blocks exist, the end of the longest block wins.
    /// Returns -1 when no day is set, and Saturday when every day is worked (no rest gap to anchor on).
    /// </summary>
    private static int FindLastWorkDayOfWeek(int mask)
    {
        mask &= 0x7F;
        if (mask == 0) return -1;
        if (mask == 0x7F) return (int)DayOfWeek.Saturday; // no rest gap; default to calendar week end

        int bestEnd = -1;
        int bestLen = -1;
        for (int d = 0; d < 7; d++)
        {
            bool isWork    = (mask & (1 << d)) != 0;
            bool nextIsRest = (mask & (1 << ((d + 1) % 7))) == 0;
            if (!isWork || !nextIsRest) continue;

            // Measure this block by walking backwards from d until a rest day is hit.
            int len = 0;
            for (int p = d; len <= 7; p--)
            {
                int day = ((p % 7) + 7) % 7;
                if ((mask & (1 << day)) == 0) break;
                len++;
            }
            if (len > bestLen) { bestLen = len; bestEnd = d; }
        }
        return bestEnd;
    }
}