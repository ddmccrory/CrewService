using CrewService.Application.Time;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.AbsenceVacancy;

public sealed class AbsenceStartProposalService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    IDepartmentAbsenceRequestWindowPolicyRepository departmentAbsenceRequestWindowPolicyRepository,
    IWorkAreaClock workAreaClock)
{
    public sealed record StartProposalResult(DateTime StartUtc, int? RequestWindowCapDays);

    public async Task<DateTime> GetProposedScheduledStartUtcAsync(
        ControlNumber employeeCtrlNbr,
        CancellationToken ct = default)
    {
        var proposal = await GetStartProposalAsync(employeeCtrlNbr, selectedLocalDay: null, ct);
        return proposal.StartUtc;
    }

    public async Task<StartProposalResult> GetStartProposalAsync(
        ControlNumber employeeCtrlNbr,
        DateOnly? selectedLocalDay = null,
        CancellationToken ct = default)
    {
        var nowUtc = DateTime.SpecifyKind(workAreaClock.UtcNow.UtcDateTime, DateTimeKind.Utc);

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var assignment = (await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr))
            .OrderByDescending(a => a.AssignedDateUtc)
            .FirstOrDefault();

        if (assignment is null)
            return new StartProposalResult(nowUtc, null);

        var staffablePosition = await uow.StaffablePositions.GetByCtrlNbrAsync(assignment.StaffablePositionCtrlNbr, ct);
        if (staffablePosition is null)
            return new StartProposalResult(nowUtc, null);

        var timeZone = await ResolveTimeZoneAsync(uow, assignment, ct);
        var nowLocal = timeZone is null
            ? nowUtc
            : TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);

        var candidateLocal = staffablePosition.PositionType switch
        {
            StaffablePositionType.Board => await ResolveBoardCandidateLocalAsync(uow, assignment, nowLocal, nowUtc, selectedLocalDay, ct),
            StaffablePositionType.Crew => await ResolveCrewCandidateLocalAsync(uow, assignment, nowLocal, nowUtc, selectedLocalDay, ct),
            _ => nowLocal
        };

        var startUtc = timeZone is null
            ? DateTime.SpecifyKind(candidateLocal, DateTimeKind.Utc)
            : TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(candidateLocal, DateTimeKind.Unspecified), timeZone);

        var requestWindowCapDays = await ResolveRequestWindowCapDaysAsync(uow, assignment, ct);
        return new StartProposalResult(startUtc, requestWindowCapDays);
    }

    private async Task<int?> ResolveRequestWindowCapDaysAsync(
        IOrchestrationUnitOfWork uow,
        PositionAssignment assignment,
        CancellationToken ct)
    {
        var departmentCtrlNbr = await ResolveDepartmentCtrlNbrAsync(uow, assignment, ct);
        if (departmentCtrlNbr is null)
            return null;

        var policy = await departmentAbsenceRequestWindowPolicyRepository.GetByDepartmentAsync(departmentCtrlNbr);
        return policy is { RequestWindowCapDays: > 0 }
            ? policy.RequestWindowCapDays
            : null;
    }

    private static async Task<ControlNumber?> ResolveDepartmentCtrlNbrAsync(
        IOrchestrationUnitOfWork uow,
        PositionAssignment assignment,
        CancellationToken ct)
    {
        var staffablePosition = await uow.StaffablePositions.GetByCtrlNbrAsync(assignment.StaffablePositionCtrlNbr, ct);
        if (staffablePosition is null)
            return null;

        if (staffablePosition.PositionType == StaffablePositionType.Crew)
        {
            var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(assignment.StaffablePositionCtrlNbr);
            if (crewPosition is null)
                return null;

            var crew = await uow.Crews.GetByCtrlNbrAsync(crewPosition.CrewCtrlNbr, ct);
            return crew?.DepartmentCtrlNbr;
        }

        if (staffablePosition.PositionType == StaffablePositionType.Board)
        {
            var board = await ResolveBoardAsync(uow, assignment, ct);
            if (board is null)
                return null;

            var boardCraft = await uow.Crafts.GetByCtrlNbrAsync(board.CraftCtrlNbr, ct);
            return boardCraft?.DepartmentCtrlNbr;
        }

        return null;
    }

    private static async Task<DateTime> ResolveBoardCandidateLocalAsync(
        IOrchestrationUnitOfWork uow,
        PositionAssignment assignment,
        DateTime nowLocal,
        DateTime nowUtc,
        DateOnly? selectedLocalDay,
        CancellationToken ct)
    {
        var selectedDate = selectedLocalDay?.ToDateTime(TimeOnly.MinValue).Date;
        if (selectedDate.HasValue && selectedDate.Value > nowLocal.Date)
            return selectedDate.Value.AddMinutes(1);

        var board = await ResolveBoardAsync(uow, assignment, ct);
        if (board is null)
            return nowLocal;

        if (board.BoardType is not (BoardType.ExtraBoard or BoardType.Hangout))
            return nowLocal;

        var scheduleContext = await ResolveScheduleContextAsync(uow, assignment.StaffablePositionCtrlNbr, nowLocal, nowUtc, nowLocal, board.CraftCtrlNbr, ct);
        if (scheduleContext is null || scheduleContext.Value.CutoffMinutes <= 0)
            return nowLocal;

        var cutoff = scheduleContext.Value.OnDutyTime.AddMinutes(-scheduleContext.Value.CutoffMinutes);
        return nowLocal.TimeOfDay > cutoff.ToTimeSpan()
            ? nowLocal.Date.AddDays(1).AddMinutes(1)
            : nowLocal;
    }

    private static async Task<DateTime> ResolveCrewCandidateLocalAsync(
        IOrchestrationUnitOfWork uow,
        PositionAssignment assignment,
        DateTime nowLocal,
        DateTime nowUtc,
        DateOnly? selectedLocalDay,
        CancellationToken ct)
    {
        var selectedDate = selectedLocalDay?.ToDateTime(TimeOnly.MinValue).Date ?? nowLocal.Date;
        var todayDate = nowLocal.Date;

        var scheduleContext = await ResolveScheduleContextAsync(uow, assignment.StaffablePositionCtrlNbr, nowLocal, nowUtc, selectedDate, craftCtrlNbr: null, ct);
        if (scheduleContext is null)
        {
            if (selectedLocalDay.HasValue)
                return selectedLocalDay.Value.ToDateTime(TimeOnly.MinValue).AddMinutes(1);

            return nowLocal;
        }

        if (selectedDate != todayDate)
        {
            var localDate = selectedDate;

            for (var i = 0; i < 14; i++)
            {
                if (IsOperatingDay(scheduleContext.Value.OperatingDaysMask, localDate.DayOfWeek))
                    return localDate.AddMinutes(1);

                localDate = localDate.AddDays(1);
            }

            return selectedDate.AddMinutes(1);
        }

        if (!IsOperatingDay(scheduleContext.Value.OperatingDaysMask, todayDate.DayOfWeek))
        {
            var nextOperatingDate = todayDate;
            for (var i = 0; i < 14; i++)
            {
                if (IsOperatingDay(scheduleContext.Value.OperatingDaysMask, nextOperatingDate.DayOfWeek))
                    return nextOperatingDate.AddMinutes(1);

                nextOperatingDate = nextOperatingDate.AddDays(1);
            }

            return todayDate.AddMinutes(1);
        }

        if (scheduleContext.Value.OnDutyTime.ToTimeSpan() < nowLocal.TimeOfDay)
            return todayDate.AddDays(1).AddMinutes(1);

        return nowLocal;
    }

    private static async Task<(int OperatingDaysMask, TimeOnly OnDutyTime, int CutoffMinutes)?> ResolveScheduleContextAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber staffablePositionCtrlNbr,
        DateTime nowLocal,
        DateTime nowUtc,
        DateTime referenceLocalDate,
        ControlNumber? craftCtrlNbr,
        CancellationToken ct)
    {
        var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(staffablePositionCtrlNbr);
        if (crewPosition is null)
            return null;

        var crewAssignments = await uow.CrewAssignments.GetByCrewAsync(crewPosition.CrewCtrlNbr);
        var activeCrewAssignments = crewAssignments
            .Where(ca => ca.StartUtc <= nowUtc && (ca.EndUtc is null || ca.EndUtc > nowUtc))
            .OrderByDescending(ca => ca.StartUtc)
            .ToList();

        var referenceDayBit = 1 << (int)referenceLocalDate.DayOfWeek;
        var activeCrewAssignment = activeCrewAssignments
            .FirstOrDefault(ca => (ca.DaysOfWeekMask & referenceDayBit) != 0);

        if (activeCrewAssignment is null)
            return null;

        var schedules = await uow.AssignmentSchedules.GetByAssignmentAsync(activeCrewAssignment.AssignmentCtrlNbr);
        var schedule = schedules
            .FirstOrDefault(s => IsOperatingDay(s.OperatingDaysMask, referenceLocalDate.DayOfWeek))
            ?? schedules.OrderBy(s => s.OnDutyTime).FirstOrDefault();

        if (schedule is null)
            return null;

        var effectiveCraftCtrlNbr = craftCtrlNbr;
        if (effectiveCraftCtrlNbr is null)
        {
            var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(crewPosition.CraftRoleCtrlNbr, ct);
            effectiveCraftCtrlNbr = craftRole?.CraftCtrlNbr;
        }

        var cutoffMinutes = 0;
        if (effectiveCraftCtrlNbr is not null)
        {
            var craftRule = await uow.CraftCallSheetRules.GetByCraftAsync(effectiveCraftCtrlNbr);
            if (craftRule is { IsEnabled: true })
                cutoffMinutes = Math.Max(0, craftRule.PreOnDutyChangeCutoffMinutes);
        }

        var effectiveOperatingDaysMask = schedule.OperatingDaysMask & activeCrewAssignment.DaysOfWeekMask;
        if (effectiveOperatingDaysMask == 0)
            return null;

        return (effectiveOperatingDaysMask, schedule.OnDutyTime, cutoffMinutes);
    }

    private async Task<TimeZoneInfo?> ResolveTimeZoneAsync(
        IOrchestrationUnitOfWork uow,
        PositionAssignment assignment,
        CancellationToken ct)
    {
        var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(assignment.StaffablePositionCtrlNbr);
        if (crewPosition is not null)
        {
            var crew = await uow.Crews.GetByCtrlNbrAsync(crewPosition.CrewCtrlNbr, ct);
            if (crew is not null)
                return await workAreaClock.GetWorkAreaTimeZoneAsync(uow, crew.WorkAreaCtrlNbr, ct);
        }

        var board = await ResolveBoardAsync(uow, assignment, ct);
        if (board is null)
            return null;

        var roster = await uow.Rosters.GetByCtrlNbrAsync(board.RosterCtrlNbr, ct);
        if (roster is null)
            return null;

        return await workAreaClock.GetWorkAreaTimeZoneAsync(uow, roster.WorkAreaGroupCtrlNbr, ct);
    }

    private static async Task<RosterBoard?> ResolveBoardAsync(
        IOrchestrationUnitOfWork uow,
        PositionAssignment assignment,
        CancellationToken ct)
    {
        if (assignment.AssignmentSourceCtrlNbr is not null)
        {
            var bySource = await uow.RosterBoards.GetByPositionCtrlNbrAsync(assignment.AssignmentSourceCtrlNbr, ct);
            if (bySource is not null)
                return bySource;
        }

        return await uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(assignment.StaffablePositionCtrlNbr, ct);
    }

    private static bool IsOperatingDay(int operatingDaysMask, DayOfWeek dayOfWeek) =>
        (operatingDaysMask & (1 << (int)dayOfWeek)) != 0;
}
