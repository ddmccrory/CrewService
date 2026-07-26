using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using CrewService.Application.Absence;
using CrewService.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.AbsenceVacancy;

public sealed class AbsenceRequestService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    IAbsenceCodeRepository absenceCodeRepository,
    IAbsenceApprovalPolicyResolver approvalPolicyResolver,
    IAbsenceRequestWaitListRecordRepository waitListRecordRepository,
    IDepartmentAbsenceWaitListPolicyRepository departmentWaitListPolicyRepository,
    IAbsenceWaitListAllowancePolicyRepository waitListAllowancePolicyRepository,
    BackgroundWorkers.IWaitListReassignmentSignal waitListReassignmentSignal,
    AbsenceStartProposalService absenceStartProposalService,
    BackgroundWorkers.IAbsenceMarkOffSignal absenceMarkOffSignal,
    BackgroundWorkers.IAutoMarkUpSignal autoMarkUpSignal,
    EmployeeNotificationService employeeNotificationService,
    ILogger<AbsenceRequestService> logger)
{
    public const long SystemApprovalOfficerCtrlNbr = 1;

    public sealed record SubmitWithCodeResult(
        AbsenceRequest? AbsenceRequest,
        AbsenceRequestWaitListRecord? WaitListRecord)
    {
        public bool IsWaitListed => WaitListRecord is not null;
    }

    private sealed record WaitListContext(
        ControlNumber? CraftCtrlNbr,
        ControlNumber? DepartmentCtrlNbr,
        ControlNumber? RailroadCtrlNbr);

    private sealed record WaitListDecision(
        bool ShouldWaitList,
        string? WaitListType);

    public async Task<AbsenceRequest> SubmitAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime? endUtc, string reasonCode, string? notes)
    {
        var absence = AbsenceRequest.Create(employeeCtrlNbr, startUtc, endUtc, reasonCode, notes);
        await using var uow = await uowFactory.CreateAsync();
        uow.AbsenceRequests.Add(absence);
        await uow.CommitAsync();
        return absence;
    }

    public Task<DateTime> GetProposedScheduledStartUtcAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default) =>
        absenceStartProposalService.GetProposedScheduledStartUtcAsync(employeeCtrlNbr, ct);

    public Task<AbsenceStartProposalService.StartProposalResult> GetStartProposalAsync(
        ControlNumber employeeCtrlNbr,
        CancellationToken ct = default) =>
        absenceStartProposalService.GetStartProposalAsync(employeeCtrlNbr, ct);

    public async Task<AbsenceRequest> SetAutoMarkOffOnApprovalAsync(ControlNumber ctrlNbr, bool enabled)
    {
        await using var uow = await uowFactory.CreateAsync();
        var absence = await uow.AbsenceRequests.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Absence request {ctrlNbr} not found.");

        absence.SetAutoMarkOffOnApproval(enabled);
        uow.AbsenceRequests.Update(absence);
        await uow.CommitAsync();

        if (enabled
            && absence.ApprovedAtUtc.HasValue
            && absence.DeniedAtUtc is null
            && absence.CancelledAtUtc is null)
        {
            absenceMarkOffSignal.Notify(absence.ScheduledStartUtc);
        }

        NotifyAutoMarkUpIfScheduledEnd(absence);
        return absence;
    }

    public async Task<AbsenceRequest> EndAbsenceAsync(ControlNumber ctrlNbr, DateTime endedUtc)
    {
        await using var uow = await uowFactory.CreateAsync();
        var absence = await uow.AbsenceRequests.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Absence request {ctrlNbr} not found.");

        var effectiveEndUtc = DateTime.SpecifyKind(endedUtc, DateTimeKind.Utc);
        var nowUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        if (effectiveEndUtc > nowUtc)
        {
            absence.ScheduleEnd(effectiveEndUtc);
        }
        else
        {
            absence.Complete(effectiveEndUtc);
        }

        uow.AbsenceRequests.Update(absence);
        await uow.CommitAsync();

        NotifyAutoMarkUpIfScheduledEnd(absence);
        return absence;
    }

    public async Task<int> ExecuteDueScheduledEndAsync(DateTime asOfUtc, CancellationToken ct = default)
    {
        var asOf = DateTime.SpecifyKind(asOfUtc, DateTimeKind.Utc);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var due = await uow.AbsenceRequests.GetScheduledEndDueAsync(asOf, ct);

        if (due.Count == 0)
            return 0;

        foreach (var request in due)
        {
            request.Complete(DateTime.SpecifyKind(request.ScheduledEndUtc!.Value, DateTimeKind.Utc));
            uow.AbsenceRequests.Update(request);
        }

        await uow.CommitAsync(ct);
        return due.Count;
    }

    public async Task<DateTime?> GetNextScheduledEndUtcAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.AbsenceRequests.GetNextScheduledEndUtcAsync(ct);
    }

    public async Task<SubmitWithCodeResult> SubmitWithCodeAsync(
        ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime? endUtc,
        ControlNumber absenceCodeCtrlNbr, string reasonCode,
        bool isSystemGenerated = false, string? notes = null,
        ControlNumber? approvedByCtrlNbr = null,
        bool autoMarkOffOnApproval = false,
        DateTime? markOffStartUtc = null,
        bool bypassWaitList = false)
    {
        var absenceCode = await absenceCodeRepository.GetByCtrlNbrAsync(absenceCodeCtrlNbr)
            ?? throw new KeyNotFoundException($"Absence code {absenceCodeCtrlNbr.Value} not found.");
        var approvalPolicy = await approvalPolicyResolver.ResolveAsync(absenceCode);

        var normalizedStartUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);

        if (!bypassWaitList)
        {
            await using var waitListUow = await uowFactory.CreateAsync();
            var context = await ResolveWaitListContextAsync(waitListUow, employeeCtrlNbr, normalizedStartUtc);
            var waitListDecision = await EvaluateWaitListDecisionAsync(
                waitListUow,
                context.CraftCtrlNbr,
                context.DepartmentCtrlNbr,
                absenceCode,
                normalizedStartUtc,
                ct: default);

            if (waitListDecision.ShouldWaitList)
            {
                if (context.DepartmentCtrlNbr is null)
                    throw new InvalidOperationException("Department context is required to place request on waitlist.");

                if (context.CraftCtrlNbr is null)
                    throw new InvalidOperationException("Craft context is required to place request on waitlist.");

                var waitListRecord = waitListDecision.WaitListType == AbsenceRequestWaitListType.VacationWeek
                    ? AbsenceRequestWaitListRecord.CreateVacationWeek(
                        employeeCtrlNbr,
                        absenceCodeCtrlNbr,
                        normalizedStartUtc,
                        DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                        context.CraftCtrlNbr,
                        context.DepartmentCtrlNbr)
                    : AbsenceRequestWaitListRecord.CreateCompensableDay(
                        employeeCtrlNbr,
                        absenceCodeCtrlNbr,
                        normalizedStartUtc,
                        DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                        context.CraftCtrlNbr,
                        context.DepartmentCtrlNbr);

                await waitListRecordRepository.AddAsync(waitListRecord);
                return new SubmitWithCodeResult(null, waitListRecord);
            }
        }

        var absence = AbsenceRequest.CreateWithCode(
            employeeCtrlNbr, startUtc, endUtc, absenceCodeCtrlNbr, reasonCode,
            isSystemGenerated, notes, autoMarkOffOnApproval);

        var markOffReferenceUtc = DateTime.SpecifyKind(markOffStartUtc ?? DateTime.UtcNow, DateTimeKind.Utc);

        if (approvalPolicy.Level == AbsenceApprovalLevel.Automatic)
        {
            var systemOfficerCtrlNbr = ControlNumber.Create(SystemApprovalOfficerCtrlNbr);
            absence.Approve(systemOfficerCtrlNbr);
        }
        else if (approvedByCtrlNbr is not null)
        {
            absence.Approve(approvedByCtrlNbr);
        }

        if (autoMarkOffOnApproval
            && absence.ApprovedAtUtc.HasValue
            && absence.DeniedAtUtc is null
            && absence.CancelledAtUtc is null
            && ShouldAutoMarkOffImmediately(absence.ScheduledStartUtc, markOffReferenceUtc, approvalPolicy))
        {
            absence.Exercise(markOffReferenceUtc);
        }

        await using var uow = await uowFactory.CreateAsync();
        uow.AbsenceRequests.Add(absence);
        await uow.CommitAsync();

        if (autoMarkOffOnApproval
            && absence.ApprovedAtUtc.HasValue
            && absence.DeniedAtUtc is null
            && absence.CancelledAtUtc is null)
        {
            absenceMarkOffSignal.Notify(absence.ScheduledStartUtc);
        }

        NotifyAutoMarkUpIfScheduledEnd(absence);

        return new SubmitWithCodeResult(absence, null);
    }

    private async Task<WaitListContext> ResolveWaitListContextAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        DateTime referenceUtc)
    {
        var assignment = (await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr))
            .Where(a => a.AssignedDateUtc <= referenceUtc)
            .OrderByDescending(a => a.AssignedDateUtc)
            .FirstOrDefault();

        if (assignment is null)
            return new WaitListContext(null, null, null);

        var staffablePosition = await uow.StaffablePositions.GetByCtrlNbrAsync(assignment.StaffablePositionCtrlNbr, default);
        if (staffablePosition is null)
            return new WaitListContext(null, null, null);

        if (staffablePosition.PositionType == StaffablePositionType.Crew)
        {
            var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(assignment.StaffablePositionCtrlNbr);
            if (crewPosition is null)
                return new WaitListContext(null, null, null);

            var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(crewPosition.CraftRoleCtrlNbr, default);
            if (craftRole is null)
                return new WaitListContext(null, null, null);

            var craft = await uow.Crafts.GetByCtrlNbrAsync(craftRole.CraftCtrlNbr, default);
            return new WaitListContext(craftRole.CraftCtrlNbr, craft?.DepartmentCtrlNbr, craft?.DynamicGroupCtrlNbr);
        }

        if (staffablePosition.PositionType == StaffablePositionType.Board)
        {
            RosterBoard? board = null;
            if (assignment.AssignmentSourceCtrlNbr is not null)
                board = await uow.RosterBoards.GetByPositionCtrlNbrAsync(assignment.AssignmentSourceCtrlNbr, default);

            board ??= await uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(assignment.StaffablePositionCtrlNbr, default);
            if (board is null)
                return new WaitListContext(null, null, null);

            var craft = await uow.Crafts.GetByCtrlNbrAsync(board.CraftCtrlNbr, default);
            return new WaitListContext(board.CraftCtrlNbr, craft?.DepartmentCtrlNbr, craft?.DynamicGroupCtrlNbr);
        }

        return new WaitListContext(null, null, null);
    }

    private async Task<WaitListDecision> EvaluateWaitListDecisionAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber? craftCtrlNbr,
        ControlNumber? departmentCtrlNbr,
        Domain.Modules.AbsenceVacancy.AbsenceCode absenceCode,
        DateTime requestStartUtc,
        CancellationToken ct)
    {
        if (craftCtrlNbr is null || departmentCtrlNbr is null)
            return new WaitListDecision(false, null);

        var departmentPolicy = await departmentWaitListPolicyRepository.GetByDepartmentAsync(departmentCtrlNbr);
        if (departmentPolicy is null || !departmentPolicy.IsEnabled)
            return new WaitListDecision(false, null);

        var allowanceCode = (absenceCode.Code ?? string.Empty).Trim().ToUpperInvariant();
        var isVacationWeek = IsVacationWeekCode(allowanceCode);
        var waitListType = isVacationWeek
            ? AbsenceRequestWaitListType.VacationWeek
            : AbsenceRequestWaitListType.CompensableDay;

        var allowance = await waitListAllowancePolicyRepository.GetByCraftTypeCodeYearAsync(
            craftCtrlNbr,
            waitListType,
            isVacationWeek ? "VW" : allowanceCode,
            requestStartUtc.Year);

        if (allowance is null || !allowance.IsEnabled)
            return new WaitListDecision(false, null);

        var craft = await uow.Crafts.GetByCtrlNbrAsync(craftCtrlNbr, ct);
        if (craft?.DynamicGroupCtrlNbr is null)
            return new WaitListDecision(false, null);

        var targetDate = requestStartUtc.Date;
        if (!isVacationWeek)
        {
            var existingRequests = await uow.AbsenceRequests.GetByDateRangeAsync(
                craft.DynamicGroupCtrlNbr,
                targetDate,
                targetDate.AddDays(1),
                includeAllStatuses: true,
                craftCtrlNbr: craftCtrlNbr,
                departmentCtrlNbr: departmentCtrlNbr,
                ct: ct);

            var currentAssigned = 0;
            foreach (var existingRequest in existingRequests)
            {
                if (existingRequest.DeniedAtUtc.HasValue || existingRequest.CancelledAtUtc.HasValue)
                    continue;

                if (existingRequest.AbsenceCodeCtrlNbr is null)
                    continue;

                var existingCode = await absenceCodeRepository.GetByCtrlNbrAsync(existingRequest.AbsenceCodeCtrlNbr, ct);
                if (existingCode is null)
                    continue;

                if (!string.Equals(existingCode.Code, allowanceCode, StringComparison.OrdinalIgnoreCase))
                    continue;

                currentAssigned++;
            }

            var maxAssignments = Math.Min(allowance.MaxAssignments, departmentPolicy.CompensableDayMaxAssignments);
            return new WaitListDecision(currentAssigned >= maxAssignments, waitListType);
        }

        var vacationWeeks = ResolveVacationWeeks(allowanceCode);
        var rangeStartUtc = targetDate.AddDays(-35);
        var rangeEndUtc = targetDate.AddDays((vacationWeeks * 7) + 1);
        var vacationRequests = await uow.AbsenceRequests.GetByDateRangeAsync(
            craft.DynamicGroupCtrlNbr,
            rangeStartUtc,
            rangeEndUtc,
            includeAllStatuses: true,
            craftCtrlNbr: craftCtrlNbr,
            departmentCtrlNbr: departmentCtrlNbr,
            ct: ct);

        var maxVacationAssignments = Math.Min(allowance.MaxAssignments, departmentPolicy.VacationWeekMaxAssignments);
        for (var i = 0; i < vacationWeeks; i++)
        {
            var weekDate = targetDate.AddDays(i * 7);
            var assignedCount = 0;

            foreach (var request in vacationRequests)
            {
                if (request.DeniedAtUtc.HasValue || request.CancelledAtUtc.HasValue)
                    continue;

                if (request.AbsenceCodeCtrlNbr is null)
                    continue;

                var existingCode = await absenceCodeRepository.GetByCtrlNbrAsync(request.AbsenceCodeCtrlNbr, ct);
                if (existingCode is null)
                    continue;

                if (!CoversVacationWeekDate(request, weekDate, existingCode.Code))
                    continue;

                assignedCount++;
            }

            if (assignedCount >= maxVacationAssignments)
                return new WaitListDecision(true, waitListType);
        }

        return new WaitListDecision(false, waitListType);
    }

    private static bool IsVacationWeekCode(string absenceCode)
    {
        if (string.IsNullOrWhiteSpace(absenceCode))
            return false;

        var normalized = absenceCode.Trim().ToUpperInvariant();
        return normalized.StartsWith('V') && !string.Equals(normalized, "VD", StringComparison.OrdinalIgnoreCase);
    }

    private static int ResolveVacationWeeks(string absenceCode)
    {
        if (string.IsNullOrWhiteSpace(absenceCode))
            return 0;

        var normalized = absenceCode.Trim().ToUpperInvariant();
        if (!normalized.StartsWith('V') || normalized == "VD")
            return 0;

        if (normalized.Length < 2)
            return 0;

        return int.TryParse(normalized[1].ToString(), out var weeks)
            ? Math.Max(weeks, 0)
            : 0;
    }

    private static bool CoversVacationWeekDate(AbsenceRequest request, DateTime weekDateUtc, string absenceCode)
    {
        var weeks = ResolveVacationWeeks(absenceCode);
        if (weeks <= 0)
            return false;

        var requestDate = DateTime.SpecifyKind(request.ScheduledStartUtc, DateTimeKind.Utc).Date;
        for (var i = 0; i < weeks; i++)
        {
            if (requestDate.AddDays(i * 7) == weekDateUtc)
                return true;
        }

        return false;
    }

    public async Task<AbsenceApprovalPolicy> ResolveApprovalPolicyAsync(ControlNumber absenceCodeCtrlNbr, CancellationToken ct = default)
    {
        var absenceCode = await absenceCodeRepository.GetByCtrlNbrAsync(absenceCodeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Absence code {absenceCodeCtrlNbr.Value} not found.");

        return await approvalPolicyResolver.ResolveAsync(absenceCode, ct);
    }

    public async Task<List<AbsenceApprovalOfficer>> GetApprovalOfficersAsync(
        ControlNumber parentCtrlNbr,
        ControlNumber railroadCtrlNbr,
        AbsenceApprovalLevel level,
        CancellationToken ct = default)
    {
        if (level == AbsenceApprovalLevel.Automatic)
        {
            return
            [
                new AbsenceApprovalOfficer(
                    SystemApprovalOfficerCtrlNbr,
                    "SYSTEM",
                    "SYSTEM",
                    "SYSTEM",
                    null)
            ];
        }

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var assignments = await uow.UserParentAssignments.GetByParentCtrlNbrAsync(parentCtrlNbr);

        var scopedAssignments = assignments
            .Where(a => a.RailroadCtrlNbr is null || a.RailroadCtrlNbr == railroadCtrlNbr)
            .ToList();

        var distinctRoleNames = scopedAssignments
            .Select(a => a.Role)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var roleMap = new Dictionary<string, CrewService.Domain.Modules.Authorization.Role>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleName in distinctRoleNames)
        {
            var role = await uow.Roles.GetByNameAsync(roleName, ct);
            if (role is not null)
                roleMap[roleName] = role;
        }

        scopedAssignments = scopedAssignments
            .Where(a => IsEligibleApproverRole(a.Role, level, roleMap))
            .ToList();

        if (scopedAssignments.Count == 0)
            return [];

        var userIds = scopedAssignments
            .Select(a => a.UserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (userIds.Count == 0)
            return [];

        var officers = new List<AbsenceApprovalOfficer>();
        foreach (var userId in userIds)
        {
            var employee = await uow.Employees.GetByUserIdAsync(userId, ct);
            if (employee is null)
                continue;

            var assignment = scopedAssignments.FirstOrDefault(a => string.Equals(a.UserId, userId, StringComparison.Ordinal));
            var role = assignment?.Role;

            officers.Add(new AbsenceApprovalOfficer(
                employee.CtrlNbr.Value,
                employee.EmployeeNumber,
                employee.EmployeeNumber,
                employee.EmployeeNumber,
                role));
        }

        return officers
            .OrderBy(o => o.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(o => o.OfficerCtrlNbr)
            .ToList();
    }

    public async Task<List<AbsenceRequest>> GetPendingAsync()
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.AbsenceRequests.GetPendingAsync();
    }

    public async Task<List<AbsenceRequest>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.AbsenceRequests.GetByEmployeeAsync(employeeCtrlNbr);
    }

    public async Task<List<AbsenceRequest>> GetByDateAsync(
        ControlNumber railroadCtrlNbr,
        DateTime requestDateUtc,
        bool includeAllStatuses,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.AbsenceRequests.GetByDateAsync(railroadCtrlNbr, requestDateUtc, includeAllStatuses, ct);
    }

    public async Task<List<AbsenceRequest>> GetByDateRangeAsync(
        ControlNumber railroadCtrlNbr,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        bool includeAllStatuses,
        ControlNumber? craftCtrlNbr = null,
        ControlNumber? departmentCtrlNbr = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.AbsenceRequests.GetByDateRangeAsync(railroadCtrlNbr, rangeStartUtc, rangeEndUtc, includeAllStatuses, craftCtrlNbr, departmentCtrlNbr, ct);
    }

    public async Task<List<AbsenceRequest>> GetOpenAbsencesByRangeAsync(
        ControlNumber railroadCtrlNbr,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        ControlNumber? craftCtrlNbr = null,
        ControlNumber? departmentCtrlNbr = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.AbsenceRequests.GetOpenAbsencesByRangeAsync(railroadCtrlNbr, rangeStartUtc, rangeEndUtc, craftCtrlNbr, departmentCtrlNbr, ct);
    }

    public async Task<AbsenceRequest> ApproveAsync(ControlNumber ctrlNbr, ControlNumber approvedByCtrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        var absence = await uow.AbsenceRequests.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Absence request {ctrlNbr} not found.");

        Domain.Modules.AbsenceVacancy.AbsenceCode? absenceCode = null;
        if (absence.AbsenceCodeCtrlNbr is not null)
        {
            absenceCode = await absenceCodeRepository.GetByCtrlNbrAsync(absence.AbsenceCodeCtrlNbr)
                ?? throw new KeyNotFoundException($"Absence code {absence.AbsenceCodeCtrlNbr.Value} not found.");
        }

        var approvalPolicy = absenceCode is null
            ? Application.AbsenceVacancy.AbsenceApprovalPolicy.ForLevel(AbsenceApprovalLevel.CallerManager)
            : await approvalPolicyResolver.ResolveAsync(absenceCode);

        absence.Approve(approvedByCtrlNbr);

        if (absence.AutoMarkOffOnApproval
            && ShouldAutoMarkOffImmediately(absence.ScheduledStartUtc, DateTime.UtcNow, approvalPolicy))
        {
            absence.Exercise(DateTime.UtcNow);
        }

        uow.AbsenceRequests.Update(absence);
        await uow.CommitAsync();

        // Always wake mark-off processing after an approval decision so the worker
        // can immediately recompute due auto mark-off work.
        absenceMarkOffSignal.Notify(DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc));

        if (absence.AutoMarkOffOnApproval
            && absence.ApprovedAtUtc.HasValue
            && absence.DeniedAtUtc is null
            && absence.CancelledAtUtc is null)
        {
            absenceMarkOffSignal.Notify(absence.ScheduledStartUtc);
        }

        NotifyAutoMarkUpIfScheduledEnd(absence);

        return absence;
    }

    public async Task<AbsenceRequest> DenyAsync(ControlNumber ctrlNbr, ControlNumber deniedByCtrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        var absence = await uow.AbsenceRequests.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Absence request {ctrlNbr} not found.");
        absence.Deny(deniedByCtrlNbr);
        uow.AbsenceRequests.Update(absence);
        await uow.CommitAsync();
        return absence;
    }

    public async Task<int> ExecuteDueAutoMarkOffAsync(DateTime asOfUtc, CancellationToken ct = default)
    {
        var asOf = DateTime.SpecifyKind(asOfUtc, DateTimeKind.Utc);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var due = await uow.AbsenceRequests.GetApprovedAutoMarkOffDueAsync(asOf, ct);
        if (due.Count == 0)
            return 0;

        foreach (var request in due)
        {
            request.Exercise(asOf);
            uow.AbsenceRequests.Update(request);
        }

        await uow.CommitAsync(ct);

        foreach (var request in due)
            NotifyAutoMarkUpIfScheduledEnd(request);

        return due.Count;
    }

    public async Task<DateTime?> GetNextApprovedAutoMarkOffStartUtcAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.AbsenceRequests.GetNextApprovedAutoMarkOffStartUtcAsync(ct);
    }

    private static bool ShouldAutoMarkOffImmediately(
        DateTime scheduledStartUtc,
        DateTime nowUtc,
        Application.AbsenceVacancy.AbsenceApprovalPolicy approvalPolicy)
    {
        var start = DateTime.SpecifyKind(scheduledStartUtc, DateTimeKind.Utc);
        var now = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);

        if (start <= now)
            return true;

        if (!approvalPolicy.AutoMarkOffIfWithinHoursEnabled)
            return false;

        if (approvalPolicy.AutoMarkOffIfWithinHours <= 0)
            return false;

        var threshold = TimeSpan.FromHours(Math.Max(0, approvalPolicy.AutoMarkOffIfWithinHours));
        return start - now <= threshold;
    }

    public async Task<AbsenceRequest> CancelAsync(ControlNumber ctrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        var absence = await uow.AbsenceRequests.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Absence request {ctrlNbr} not found.");
        absence.Cancel();
        uow.AbsenceRequests.Update(absence);
        await uow.CommitAsync();

        waitListReassignmentSignal.Notify();

        return absence;
    }

    public async Task<AbsenceRequest> MarkOffAsync(ControlNumber ctrlNbr, DateTime exercisedUtc)
    {
        await using var uow = await uowFactory.CreateAsync();
        var absence = await uow.AbsenceRequests.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Absence request {ctrlNbr} not found.");

        absence.Exercise(DateTime.SpecifyKind(exercisedUtc, DateTimeKind.Utc));
        uow.AbsenceRequests.Update(absence);
        await uow.CommitAsync();

        NotifyAutoMarkUpIfScheduledEnd(absence);

        return absence;
    }

    public async Task NotifyWaitListPromotionAsync(
        AbsenceRequest createdRequest,
        AbsenceRequestWaitListRecord waitListRecord,
        CancellationToken ct = default)
    {
        var absenceCode = await absenceCodeRepository.GetByCtrlNbrAsync(waitListRecord.AbsenceCodeCtrlNbr, ct);
        var absenceCodeDescription = absenceCode?.Description ?? absenceCode?.Code ?? "Absence";

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        ControlNumber? railroadCtrlNbr = null;
        if (waitListRecord.CraftCtrlNbr is not null)
        {
            var craft = await uow.Crafts.GetByCtrlNbrAsync(waitListRecord.CraftCtrlNbr, ct);
            railroadCtrlNbr = craft?.DynamicGroupCtrlNbr;
        }

        if (railroadCtrlNbr is null)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    "Skipping waitlist-promotion notification for absence request {AbsenceRequestCtrlNbr}: railroad could not be resolved.",
                    createdRequest.CtrlNbr.Value);
            }

            return;
        }

        await employeeNotificationService.NotifyWaitListPromotedToRequestAsync(
            uow,
            railroadCtrlNbr,
            waitListRecord.EmployeeCtrlNbr,
            absenceCodeDescription,
            createdRequest.ScheduledStartUtc,
            ct);

        await uow.CommitAsync(ct);
    }

    private void NotifyAutoMarkUpIfScheduledEnd(AbsenceRequest absence)
    {
        if (!AbsenceStatusHelper.IsOpen(absence))
            return;

        if (!absence.ScheduledEndUtc.HasValue)
            return;

        autoMarkUpSignal.Notify(DateTime.SpecifyKind(absence.ScheduledEndUtc.Value, DateTimeKind.Utc));
    }

    private static bool IsEligibleApproverRole(
        string? roleName,
        AbsenceApprovalLevel level,
        IReadOnlyDictionary<string, CrewService.Domain.Modules.Authorization.Role> roleMap)
    {
        if (string.IsNullOrWhiteSpace(roleName)
            || !roleMap.TryGetValue(roleName, out var role))
            return false;

        if (string.Equals(role.Name, Roles.SystemAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role.Name, Roles.ParentAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role.Name, Roles.RailroadAdmin, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (role.Level <= 20)
            return false;

        var isManagerRole = !string.IsNullOrWhiteSpace(role.Description)
            && role.Description.Contains("manage", StringComparison.OrdinalIgnoreCase);

        return level switch
        {
            AbsenceApprovalLevel.CallerManager => true,
            AbsenceApprovalLevel.ManagerOnly => isManagerRole,
            _ => false
        };
    }
}

public sealed record AbsenceApprovalOfficer(
    long OfficerCtrlNbr,
    string DisplayName,
    string FullName,
    string EmployeeNumber,
    string? Role);
