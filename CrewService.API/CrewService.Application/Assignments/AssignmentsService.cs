using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Assignments;

public sealed class AssignmentsService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    // ── Assignments ──────────────────────────────────────────────────────────

    public async Task<List<Assignment>> GetAssignmentsAsync(
        ControlNumber? workAreaGroupCtrlNbr, ControlNumber? departmentCtrlNbr,
        ControlNumber? railroadCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        if (workAreaGroupCtrlNbr is not null && departmentCtrlNbr is not null)
            return await uow.Assignments.GetByWorkAreaAndDepartmentAsync(workAreaGroupCtrlNbr, departmentCtrlNbr);
        if (workAreaGroupCtrlNbr is not null)
            return await uow.Assignments.GetByWorkAreaAsync(workAreaGroupCtrlNbr);
        if (railroadCtrlNbr is not null)
            return await uow.Assignments.GetAllByRailroadAsync(railroadCtrlNbr);
        return await uow.Assignments.GetAllAsync();
    }

    public async Task<Assignment> GetAssignmentAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Assignments.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Assignment {ctrlNbr.Value} not found.");
    }

    public async Task<(List<Assignment> Assignments, Dictionary<ControlNumber, int> DaysMasks, Dictionary<ControlNumber, string> OnDutyTimes, Dictionary<ControlNumber, DynamicGroup> Groups, Dictionary<ControlNumber, long> WorkAreaLookup)>
        GetAssignmentsWithDetailsAsync(
            ControlNumber? workAreaGroupCtrlNbr, ControlNumber? departmentCtrlNbr,
            ControlNumber? railroadCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        List<Assignment> assignments;
        if (workAreaGroupCtrlNbr is not null && departmentCtrlNbr is not null)
            assignments = await uow.Assignments.GetByWorkAreaAndDepartmentAsync(workAreaGroupCtrlNbr, departmentCtrlNbr);
        else if (workAreaGroupCtrlNbr is not null)
            assignments = await uow.Assignments.GetByWorkAreaAsync(workAreaGroupCtrlNbr);
        else if (railroadCtrlNbr is not null)
            assignments = await uow.Assignments.GetAllByRailroadAsync(railroadCtrlNbr);
        else
            assignments = await uow.Assignments.GetAllAsync();

        var assignmentIds = assignments.Select(a => a.CtrlNbr).ToList();
        var allSchedules = await uow.AssignmentSchedules.GetByAssignmentsAsync(assignmentIds);
        var daysMasks = allSchedules.GroupBy(s => s.AssignmentCtrlNbr)
            .ToDictionary(g => g.Key, g => g.Aggregate(0, (mask, s) => mask | s.OperatingDaysMask));
        var onDutyTimes = allSchedules.GroupBy(s => s.AssignmentCtrlNbr)
            .ToDictionary(g => g.Key, g => g.Min(s => s.OnDutyTime).ToString("HH:mm"));

        var groupCtrlNbrs = assignments.Select(a => a.GroupCtrlNbr).Distinct().ToList();
        var groups = await uow.DynamicGroups.GetByCtrlNbrsAsync(groupCtrlNbrs);
        var groupLookup = groups.ToDictionary(g => g.CtrlNbr);

        var workAreaLookup = new Dictionary<ControlNumber, long>();
        foreach (var g in groups)
        {
            if (g.IsWorkArea)
                workAreaLookup[g.CtrlNbr] = g.CtrlNbr.Value;
            else if (g.Path is not null)
            {
                var ancestors = await uow.DynamicGroups.GetAncestorsAsync(g.CtrlNbr);
                var wa = ancestors.FirstOrDefault(a => a.IsWorkArea);
                workAreaLookup[g.CtrlNbr] = wa?.CtrlNbr.Value ?? 0;
            }
        }

        return (assignments, daysMasks, onDutyTimes, groupLookup, workAreaLookup);
    }

    public async Task<(Assignment Assignment, DynamicGroup? Group, long WorkAreaCtrlNbr)>
        CreateAssignmentAsync(ControlNumber groupCtrlNbr, string code, string name,
            bool isExtra, bool isActive, ControlNumber? departmentCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var workAreaCtrlNbr = await ResolveWorkAreaCtrlNbrAsync(uow, groupCtrlNbr);
        if (workAreaCtrlNbr is not null && await uow.Assignments.ExistsByCodeInWorkAreaAsync(workAreaCtrlNbr, code))
            throw new InvalidOperationException($"Assignment code '{code.ToUpperInvariant()}' already exists in this work area.");

        var assignment = Assignment.Create(groupCtrlNbr, code, name, isExtra, isActive, departmentCtrlNbr);
        uow.Assignments.Add(assignment);
        await uow.CommitAsync(ct);

        var group = await uow.DynamicGroups.GetByCtrlNbrAsync(groupCtrlNbr);
        long waVal = workAreaCtrlNbr?.Value ?? 0;
        return (assignment, group, waVal);
    }

    public async Task<(Assignment Assignment, DynamicGroup? Group, long WorkAreaCtrlNbr)>
        UpdateAssignmentAsync(ControlNumber ctrlNbr, string code, string name,
            bool isExtra, bool isActive, ControlNumber? departmentCtrlNbr,
            ControlNumber? groupCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var assignment = await uow.Assignments.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Assignment {ctrlNbr.Value} not found.");

        var effectiveGroup = groupCtrlNbr ?? assignment.GroupCtrlNbr;
        var effectiveCode = !string.IsNullOrWhiteSpace(code) ? code : assignment.Code;
        var workAreaCtrlNbr = await ResolveWorkAreaCtrlNbrAsync(uow, effectiveGroup);
        if (workAreaCtrlNbr is not null && await uow.Assignments.ExistsByCodeInWorkAreaAsync(workAreaCtrlNbr, effectiveCode, assignment.CtrlNbr))
            throw new InvalidOperationException($"Assignment code '{effectiveCode.ToUpperInvariant()}' already exists in this work area.");

        assignment.Update(code, name, isExtra, isActive, departmentCtrlNbr, groupCtrlNbr);
        uow.Assignments.Update(assignment);
        await uow.CommitAsync(ct);

        var group = await uow.DynamicGroups.GetByCtrlNbrAsync(assignment.GroupCtrlNbr);
        long waVal = workAreaCtrlNbr?.Value ?? 0;
        return (assignment, group, waVal);
    }

    public async Task DeleteAssignmentAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var assignment = await uow.Assignments.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Assignment {ctrlNbr.Value} not found.");
        uow.Assignments.Remove(assignment);
        await uow.CommitAsync(ct);
    }

    // ── Assignment Schedules ─────────────────────────────────────────────────

    public async Task<(List<AssignmentSchedule> Schedules, Dictionary<long, string> ShiftNames)>
        GetAssignmentSchedulesAsync(ControlNumber assignmentCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var schedules = await uow.AssignmentSchedules.GetByAssignmentAsync(assignmentCtrlNbr);

        var assignment = await uow.Assignments.GetByCtrlNbrAsync(assignmentCtrlNbr);
        var shiftNames = new Dictionary<long, string>();
        if (assignment is not null)
        {
            var workAreaCtrlNbr = await ResolveWorkAreaCtrlNbrAsync(uow, assignment.GroupCtrlNbr);
            var shifts = workAreaCtrlNbr is not null ? await uow.ShiftDefinitions.GetByWorkAreaAsync(workAreaCtrlNbr) : [];
            foreach (var sd in shifts)
                shiftNames[sd.CtrlNbr.Value] = $"{sd.ShiftCode} \u2014 {sd.DisplayName}";
        }

        return (schedules, shiftNames);
    }

    public async Task<AssignmentSchedule> CreateAssignmentScheduleAsync(
        ControlNumber assignmentCtrlNbr, ControlNumber shiftDefinitionCtrlNbr,
        int operatingDaysMask, TimeOnly onDutyTime, TimeOnly offDutyTime, CancellationToken ct = default)
    {
        var schedule = AssignmentSchedule.Create(assignmentCtrlNbr, shiftDefinitionCtrlNbr, operatingDaysMask, onDutyTime, offDutyTime);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.AssignmentSchedules.Add(schedule);
        await uow.CommitAsync(ct);
        return schedule;
    }

    public async Task<AssignmentSchedule> UpdateAssignmentScheduleAsync(
        ControlNumber ctrlNbr, int operatingDaysMask, TimeOnly onDutyTime, TimeOnly offDutyTime, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var schedule = await uow.AssignmentSchedules.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"AssignmentSchedule {ctrlNbr.Value} not found.");
        schedule.Update(operatingDaysMask, onDutyTime, offDutyTime);
        uow.AssignmentSchedules.Update(schedule);
        await uow.CommitAsync(ct);
        return schedule;
    }

    public async Task DeleteAssignmentScheduleAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var schedule = await uow.AssignmentSchedules.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"AssignmentSchedule {ctrlNbr.Value} not found.");
        uow.AssignmentSchedules.Remove(schedule);
        await uow.CommitAsync(ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task<ControlNumber?> ResolveWorkAreaCtrlNbrAsync(
        IOrchestrationUnitOfWork uow, ControlNumber groupCtrlNbr)
    {
        var group = await uow.DynamicGroups.GetByCtrlNbrAsync(groupCtrlNbr);
        if (group is null) return null;
        if (group.IsWorkArea) return group.CtrlNbr;
        var ancestors = await uow.DynamicGroups.GetAncestorsAsync(groupCtrlNbr);
        return ancestors.FirstOrDefault(g => g.IsWorkArea)?.CtrlNbr;
    }
}
