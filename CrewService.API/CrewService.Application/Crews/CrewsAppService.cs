using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.Crews;

public sealed class CrewsAppService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    ILogger<CrewsAppService> logger)
{
    // ── Crews ────────────────────────────────────────────────────────────────

    public async Task<(List<Crew> Crews, Dictionary<ControlNumber, int> PositionCounts, Dictionary<ControlNumber, int> DaysMasks)>
        GetAllCrewsAsync(string? crewType, ControlNumber? workAreaCtrlNbr, ControlNumber? railroadCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        List<Crew> crews;
        if (!string.IsNullOrEmpty(crewType))
            crews = await uow.Crews.GetByTypeAsync(crewType);
        else if (workAreaCtrlNbr is not null)
            crews = await uow.Crews.GetByWorkAreaAsync(workAreaCtrlNbr);
        else if (railroadCtrlNbr is not null)
            crews = await uow.Crews.GetByRailroadAsync(railroadCtrlNbr);
        else
            crews = await uow.Crews.GetAllAsync();

        var crewIds = crews.Select(c => c.CtrlNbr).ToList();
        var allPositions = await uow.CrewPositions.GetByCrewsAsync(crewIds);
        var allAssignments = await uow.CrewAssignments.GetByCrewsAsync(crewIds);

        var positionCounts = allPositions.GroupBy(p => p.CrewCtrlNbr)
            .ToDictionary(g => g.Key, g => g.Count());
        var daysMasks = allAssignments.GroupBy(a => a.CrewCtrlNbr)
            .ToDictionary(g => g.Key, g => g.Aggregate(0, (mask, a) => mask | a.DaysOfWeekMask));

        return (crews, positionCounts, daysMasks);
    }

    public async Task<Crew> GetCrewAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Crews.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Crew {ctrlNbr.Value} not found.");
    }

    public async Task<Crew> CreateCrewAsync(
        string crewType, long workAreaCtrlNbr, string name, bool isActive,
        ControlNumber? departmentCtrlNbr, DateTime? effectiveDate, DateTime? abolishedDate,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        if (await uow.Crews.ExistsByNameInWorkAreaAsync(ControlNumber.Create(workAreaCtrlNbr), name))
            throw new InvalidOperationException($"Crew name '{name}' already exists in this work area.");
        var crew = Crew.Create(crewType, workAreaCtrlNbr, name, isActive, departmentCtrlNbr, effectiveDate, abolishedDate);
        uow.Crews.Add(crew);
        await uow.CommitAsync(ct);
        return crew;
    }

    public async Task<Crew> UpdateCrewAsync(
        ControlNumber ctrlNbr, string name, bool isActive, ControlNumber? departmentCtrlNbr,
        DateTime? effectiveDate, DateTime? abolishedDate, string? crewType, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var crew = await uow.Crews.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Crew {ctrlNbr.Value} not found.");
        if (await uow.Crews.ExistsByNameInWorkAreaAsync(crew.WorkAreaCtrlNbr, name, crew.CtrlNbr))
            throw new InvalidOperationException($"Crew name '{name}' already exists in this work area.");
        crew.Update(name, isActive, departmentCtrlNbr, effectiveDate, abolishedDate, crewType);
        uow.Crews.Update(crew);
        await uow.CommitAsync(ct);
        return crew;
    }

    public async Task DeleteCrewAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var crew = await uow.Crews.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Crew {ctrlNbr.Value} not found.");
        uow.Crews.Remove(crew);
        await uow.CommitAsync(ct);
    }

    // ── Crew Positions ───────────────────────────────────────────────────────

    public async Task<List<CrewPosition>> GetCrewPositionsAsync(ControlNumber crewCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.CrewPositions.GetByCrewAsync(crewCtrlNbr);
    }

    public async Task<CrewPosition> CreateCrewPositionAsync(
        long crewCtrlNbr, long craftRoleCtrlNbr, int displayOrder, CancellationToken ct = default)
    {
        var staffablePosition = StaffablePosition.Create(StaffablePositionType.Crew);
        var position = CrewPosition.Create(crewCtrlNbr, craftRoleCtrlNbr, displayOrder, staffablePosition.CtrlNbr);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.StaffablePositions.Add(staffablePosition);
        uow.CrewPositions.Add(position);

        // Crew positions are always bulletined when vacant. A newly created position has no incumbent.
        var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(ControlNumber.Create(craftRoleCtrlNbr), ct);
        var crew = await uow.Crews.GetByCtrlNbrAsync(ControlNumber.Create(crewCtrlNbr), ct);
        if (craftRole is not null && crew is not null)
        {
            var rule = await uow.BulletinRules.GetByCraftAsync(craftRole.CraftCtrlNbr);
            if (rule is not null)
            {
                var vacancy = PositionVacancy.Create(
                    crew.WorkAreaCtrlNbr, StaffablePositionType.Crew, staffablePosition.CtrlNbr,
                    craftRole.CraftCtrlNbr, "POSITION_CREATED",
                    targetName: $"{crew.Name} - {craftRole.Name}");
                var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(crew.WorkAreaCtrlNbr);
                var tz = string.IsNullOrWhiteSpace(workArea?.TimeZoneId) ? null : (TimeZoneInfo.TryFindSystemTimeZoneById(workArea.TimeZoneId, out var tzInfo) ? tzInfo : null);
                var (opens, closes, effective) = rule.CalculateBidWindow(DateTime.UtcNow, tz);
                var bulletin = Bulletin.Create(vacancy.CtrlNbr, craftRole.CraftCtrlNbr, opens, closes, effective);
                vacancy.MarkBulletined();
                await uow.PositionVacancies.AddAsync(vacancy, ct);
                await uow.Bulletins.AddAsync(bulletin, ct);
            }
            else
            {
                logger.LogWarning(
                    "No BulletinRule configured for craft {CraftCtrlNbr}. Bulletin not created for new crew position {StaffablePositionCtrlNbr}.",
                    craftRole.CraftCtrlNbr.Value, staffablePosition.CtrlNbr.Value);
            }
        }

        await uow.CommitAsync(ct);
        return position;
    }

    public async Task DeleteCrewPositionAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var position = await uow.CrewPositions.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"CrewPosition {ctrlNbr.Value} not found.");
        uow.CrewPositions.Remove(position);
        await uow.CommitAsync(ct);
    }

    // ── Crew Incumbencies ────────────────────────────────────────────────────

    public async Task<List<CrewIncumbency>> GetCrewIncumbenciesAsync(ControlNumber crewPositionCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.CrewIncumbencies.GetByCrewPositionAsync(crewPositionCtrlNbr);
    }

    public async Task<CrewIncumbency> CreateCrewIncumbencyAsync(
        long crewPositionCtrlNbr, long employeeCtrlNbr,
        DateTime startUtc, DateTime? endUtc, CancellationToken ct = default)
    {
        var empCtrlNbr = ControlNumber.Create(employeeCtrlNbr);
        var posCtrlNbr = ControlNumber.Create(crewPositionCtrlNbr);

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var existingAssignments = await uow.PositionAssignments.GetByEmployeeAsync(empCtrlNbr);
        if (existingAssignments.Count > 0)
            throw new InvalidOperationException(
                "This employee is already assigned to a staffable position. Unassign them first.");

        var crewPosition = await uow.CrewPositions.GetByCtrlNbrAsync(posCtrlNbr)
            ?? throw new KeyNotFoundException($"CrewPosition {crewPositionCtrlNbr} not found.");

        var incumbency = CrewIncumbency.Create(crewPositionCtrlNbr, employeeCtrlNbr, startUtc, endUtc);
        var positionAssignment = PositionAssignment.Create(
            crewPosition.StaffablePositionCtrlNbr, empCtrlNbr, PositionAssignmentType.Direct, crewPosition.CtrlNbr,
            assignedDateUtc: startUtc);

        uow.CrewIncumbencies.Add(incumbency);
        uow.PositionAssignments.Add(positionAssignment);
        await uow.CommitAsync(ct);
        return incumbency;
    }

    public async Task EndCrewIncumbencyAsync(ControlNumber ctrlNbr, DateTime endUtc, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var incumbency = await uow.CrewIncumbencies.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Incumbency {ctrlNbr.Value} not found.");
        incumbency.End(endUtc);
        uow.CrewIncumbencies.Update(incumbency);

        var crewPosition = await uow.CrewPositions.GetByCtrlNbrAsync(incumbency.CrewPositionCtrlNbr, ct);
        if (crewPosition is not null)
        {
            var positionAssignment = await uow.PositionAssignments.GetByStaffablePositionAsync(crewPosition.StaffablePositionCtrlNbr);
            if (positionAssignment is not null)
                uow.PositionAssignments.Remove(positionAssignment);

            // Crew positions are always bulletined when vacated.
            var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(crewPosition.CraftRoleCtrlNbr, ct);
            var crew = await uow.Crews.GetByCtrlNbrAsync(crewPosition.CrewCtrlNbr, ct);
            if (craftRole is not null && crew is not null)
            {
                var rule = await uow.BulletinRules.GetByCraftAsync(craftRole.CraftCtrlNbr);
                if (rule is not null)
                {
                    var vacancy = PositionVacancy.Create(
                        crew.WorkAreaCtrlNbr, StaffablePositionType.Crew, crewPosition.StaffablePositionCtrlNbr,
                        craftRole.CraftCtrlNbr, "INCUMBENT_VACATED", incumbency.EmployeeCtrlNbr,
                        targetName: $"{crew.Name} - {craftRole.Name}");
                    var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(crew.WorkAreaCtrlNbr);
                    var tz = string.IsNullOrWhiteSpace(workArea?.TimeZoneId) ? null : (TimeZoneInfo.TryFindSystemTimeZoneById(workArea.TimeZoneId, out var tzInfo) ? tzInfo : null);
                    var (opens, closes, effective) = rule.CalculateBidWindow(DateTime.UtcNow, tz);
                    var bulletin = Bulletin.Create(vacancy.CtrlNbr, craftRole.CraftCtrlNbr, opens, closes, effective);
                    vacancy.MarkBulletined();
                    await uow.PositionVacancies.AddAsync(vacancy, ct);
                    await uow.Bulletins.AddAsync(bulletin, ct);
                }
                else
                {
                    logger.LogWarning(
                        "No BulletinRule configured for craft {CraftCtrlNbr}. Bulletin not created for vacated crew position {StaffablePositionCtrlNbr}.",
                        craftRole.CraftCtrlNbr.Value, crewPosition.StaffablePositionCtrlNbr.Value);
                }
            }
        }
        await uow.CommitAsync(ct);
    }

    // ── Crew Assignments ─────────────────────────────────────────────────────

    public async Task<(List<CrewAssignment> Items, Dictionary<long, string> CrewNames)>
        GetCrewAssignmentsAsync(ControlNumber crewCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var items = await uow.CrewAssignments.GetByCrewAsync(crewCtrlNbr);
        var crewNames = await ResolveCrewNamesAsync(uow, items);
        return (items, crewNames);
    }

    public async Task<(List<CrewAssignment> Items, Dictionary<long, string> CrewNames)>
        GetCrewAssignmentsByAssignmentAsync(ControlNumber assignmentCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var items = await uow.CrewAssignments.GetByAssignmentAsync(assignmentCtrlNbr);
        var crewNames = await ResolveCrewNamesAsync(uow, items);
        return (items, crewNames);
    }

    public async Task<CrewAssignment> CreateCrewAssignmentAsync(
        long crewCtrlNbr, long assignmentCtrlNbr, int daysOfWeekMask,
        DateTime startUtc, DateTime? endUtc, CancellationToken ct = default)
    {
        var assignment = CrewAssignment.Create(crewCtrlNbr, assignmentCtrlNbr, daysOfWeekMask, startUtc, endUtc);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.CrewAssignments.Add(assignment);
        await uow.CommitAsync(ct);
        return assignment;
    }

    public async Task<CrewAssignment> UpdateCrewAssignmentAsync(
        ControlNumber ctrlNbr, int daysOfWeekMask, DateTime startUtc, DateTime? endUtc, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var assignment = await uow.CrewAssignments.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"CrewAssignment {ctrlNbr.Value} not found.");
        assignment.Update(daysOfWeekMask, startUtc, endUtc);
        uow.CrewAssignments.Update(assignment);
        await uow.CommitAsync(ct);
        return assignment;
    }

    public async Task DeleteCrewAssignmentAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var assignment = await uow.CrewAssignments.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"CrewAssignment {ctrlNbr.Value} not found.");
        uow.CrewAssignments.Remove(assignment);
        await uow.CommitAsync(ct);
    }

    private static async Task<Dictionary<long, string>> ResolveCrewNamesAsync(
        IOrchestrationUnitOfWork uow, List<CrewAssignment> items)
    {
        var crewNames = new Dictionary<long, string>();
        foreach (var ctrlNbr in items.Select(a => a.CrewCtrlNbr).Distinct())
        {
            var crew = await uow.Crews.GetByCtrlNbrAsync(ctrlNbr);
            if (crew is not null) crewNames[ctrlNbr.Value] = crew.Name;
        }
        return crewNames;
    }

    // ── Wizard ───────────────────────────────────────────────────────────────

    public sealed record WizardPositionEntry(long CraftRoleCtrlNbr, int DisplayOrder);

    public sealed record WizardAssignmentEntry(
        long ExistingAssignmentCtrlNbr, long GroupCtrlNbr, long DepartmentCtrlNbr,
        string Code, string Name, bool IsExtra,
        long ShiftDefinitionCtrlNbr, string OnDutyTime, string OffDutyTime,
        int AssignmentOperatingDaysMask, int CrewWorkDaysMask,
        string StartDate, string EndDate);

    public sealed record WizardResult(
        long CrewCtrlNbr, string CrewName,
        int AssignmentsCreated, int AssignmentsUpdated,
        int SchedulesCreated, int SchedulesUpdated, int SchedulesExisting,
        int CrewAssignmentsCreated, int CrewAssignmentsUpdated, int CrewAssignmentsDeleted, int CrewAssignmentsExisting,
        int PositionsCreated, int PositionsDeleted, int PositionsExisting,
        bool IsExistingCrew);

    public async Task<WizardResult> CrewSetupWizardAsync(
        long existingCrewCtrlNbr, long workAreaCtrlNbr, string crewName, string crewType,
        long crewDepartmentCtrlNbr, string effectiveDateStr, string abolishedDateStr,
        List<WizardPositionEntry> positions, List<WizardAssignmentEntry> assignments,
        CancellationToken ct = default)
    {
        if (assignments.Count == 0)
            throw new InvalidOperationException("At least one assignment entry is required.");

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        Crew crew;
        if (existingCrewCtrlNbr > 0)
        {
            crew = await uow.Crews.GetByCtrlNbrAsync(ControlNumber.Create(existingCrewCtrlNbr))
                ?? throw new KeyNotFoundException($"Crew {existingCrewCtrlNbr} not found.");

            var newEffective = !string.IsNullOrWhiteSpace(effectiveDateStr)
                ? DateTime.Parse(effectiveDateStr).ToUniversalTime() : crew.EffectiveDate;
            var newAbolished = !string.IsNullOrWhiteSpace(abolishedDateStr)
                ? DateTime.Parse(abolishedDateStr).ToUniversalTime() : (DateTime?)null;
            var newCrewType = !string.IsNullOrWhiteSpace(crewType) ? crewType : null;

            if (crew.EffectiveDate != newEffective || crew.AbolishedDate != newAbolished ||
                (newCrewType is not null && crew.CrewType != newCrewType))
            {
                crew.Update(crew.Name, crew.IsActive, crew.DepartmentCtrlNbr, newEffective, newAbolished, newCrewType);
                uow.Crews.Update(crew);
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(crewName))
                throw new InvalidOperationException("Crew name is required when creating a new crew.");
            if (await uow.Crews.ExistsByNameInWorkAreaAsync(ControlNumber.Create(workAreaCtrlNbr), crewName))
                throw new InvalidOperationException($"Crew name '{crewName}' already exists in this work area.");

            var deptCtrlNbr = crewDepartmentCtrlNbr > 0 ? ControlNumber.Create(crewDepartmentCtrlNbr) : null;
            var effectiveDate = !string.IsNullOrWhiteSpace(effectiveDateStr)
                ? DateTime.Parse(effectiveDateStr).ToUniversalTime() : (DateTime?)null;
            var abolishedDate = !string.IsNullOrWhiteSpace(abolishedDateStr)
                ? DateTime.Parse(abolishedDateStr).ToUniversalTime() : (DateTime?)null;
            crew = Crew.Create(crewType, workAreaCtrlNbr, crewName, isActive: true, deptCtrlNbr, effectiveDate, abolishedDate);
            uow.Crews.Add(crew);
        }

        int assignmentsCreated = 0, assignmentsUpdated = 0;
        int schedulesCreated = 0, schedulesUpdated = 0, schedulesExisting = 0;
        int crewAssignmentsCreated = 0, crewAssignmentsUpdated = 0, crewAssignmentsDeleted = 0, crewAssignmentsExisting = 0;
        int positionsCreated = 0, positionsDeleted = 0, positionsExisting = 0;
        var consumedCrewAssignmentKeys = new HashSet<long>();

        Dictionary<long, CrewAssignment> existingCrewAssignmentMap = existingCrewCtrlNbr > 0
            ? (await uow.CrewAssignments.GetByCrewAsync(crew.CtrlNbr)).ToDictionary(ca => ca.AssignmentCtrlNbr.Value)
            : new Dictionary<long, CrewAssignment>();

        var unmatchedPositions = existingCrewCtrlNbr > 0
            ? (await uow.CrewPositions.GetByCrewAsync(crew.CtrlNbr)).ToList()
            : new List<CrewPosition>();

        // Positions
        foreach (var pos in positions)
        {
            if (pos.CraftRoleCtrlNbr <= 0) continue;
            var craftRoleCtrlNbr = ControlNumber.Create(pos.CraftRoleCtrlNbr);
            var match = unmatchedPositions.FindIndex(ep => ep.CraftRoleCtrlNbr == craftRoleCtrlNbr && ep.DisplayOrder == pos.DisplayOrder);
            if (match >= 0) { unmatchedPositions.RemoveAt(match); positionsExisting++; continue; }
            var sp = StaffablePosition.Create(StaffablePositionType.Crew);
            uow.StaffablePositions.Add(sp);
            uow.CrewPositions.Add(CrewPosition.Create(crew.CtrlNbr, craftRoleCtrlNbr, pos.DisplayOrder, sp.CtrlNbr));
            positionsCreated++;

            // Crew positions are always bulletined when vacant; a newly created position has no incumbent.
            var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(craftRoleCtrlNbr, ct);
            if (craftRole is not null)
            {
                var rule = await uow.BulletinRules.GetByCraftAsync(craftRole.CraftCtrlNbr);
                if (rule is not null)
                {
                    var vacancy = PositionVacancy.Create(
                        crew.WorkAreaCtrlNbr, StaffablePositionType.Crew, sp.CtrlNbr,
                        craftRole.CraftCtrlNbr, "POSITION_CREATED",
                        targetName: $"{crew.Name} - {craftRole.Name}");
                    var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(crew.WorkAreaCtrlNbr);
                    var tz = string.IsNullOrWhiteSpace(workArea?.TimeZoneId) ? null : (TimeZoneInfo.TryFindSystemTimeZoneById(workArea.TimeZoneId, out var tzInfo) ? tzInfo : null);
                    var (opens, closes, effective) = rule.CalculateBidWindow(DateTime.UtcNow, tz);
                    var bulletin = Bulletin.Create(vacancy.CtrlNbr, craftRole.CraftCtrlNbr, opens, closes, effective);
                    vacancy.MarkBulletined();
                    uow.PositionVacancies.Add(vacancy);
                    uow.Bulletins.Add(bulletin);
                }
                else
                {
                    logger.LogWarning(
                        "No BulletinRule configured for craft {CraftCtrlNbr}. Bulletin not created for new crew position {StaffablePositionCtrlNbr}.",
                        craftRole.CraftCtrlNbr.Value, sp.CtrlNbr.Value);
                }
            }
        }
        foreach (var removed in unmatchedPositions) { uow.CrewPositions.Remove(removed); positionsDeleted++; }

        // Assignments, Schedules, CrewAssignments
        foreach (var entry in assignments)
        {
            Assignment assignment;
            if (entry.ExistingAssignmentCtrlNbr > 0)
            {
                assignment = await uow.Assignments.GetByCtrlNbrAsync(ControlNumber.Create(entry.ExistingAssignmentCtrlNbr))
                    ?? throw new KeyNotFoundException($"Assignment {entry.ExistingAssignmentCtrlNbr} not found.");

                var effectiveCode = !string.IsNullOrWhiteSpace(entry.Code) ? entry.Code : assignment.Code;
                var effectiveGroup = entry.GroupCtrlNbr > 0 ? ControlNumber.Create(entry.GroupCtrlNbr) : assignment.GroupCtrlNbr;
                var waCtrlNbr = await ResolveWorkAreaCtrlNbrAsync(uow, effectiveGroup);
                if (waCtrlNbr is not null && await uow.Assignments.ExistsByCodeInWorkAreaAsync(waCtrlNbr, effectiveCode, assignment.CtrlNbr))
                    throw new InvalidOperationException($"Assignment code '{effectiveCode.ToUpperInvariant()}' already exists in this work area.");

                var deptCtrlNbr = entry.DepartmentCtrlNbr > 0 ? ControlNumber.Create(entry.DepartmentCtrlNbr) : null;
                assignment.Update(
                    code: !string.IsNullOrWhiteSpace(entry.Code) ? entry.Code : null,
                    name: !string.IsNullOrWhiteSpace(entry.Name) ? entry.Name : null,
                    isExtra: entry.IsExtra, isActive: true,
                    departmentCtrlNbr: deptCtrlNbr,
                    groupCtrlNbr: entry.GroupCtrlNbr > 0 ? ControlNumber.Create(entry.GroupCtrlNbr) : null);
                uow.Assignments.Update(assignment);
                assignmentsUpdated++;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(entry.Code) || string.IsNullOrWhiteSpace(entry.Name))
                    throw new InvalidOperationException("Assignment code and name are required for new assignments.");

                var waCtrlNbr = await ResolveWorkAreaCtrlNbrAsync(uow, ControlNumber.Create(entry.GroupCtrlNbr));
                if (waCtrlNbr is not null && await uow.Assignments.ExistsByCodeInWorkAreaAsync(waCtrlNbr, entry.Code))
                    throw new InvalidOperationException($"Assignment code '{entry.Code.ToUpperInvariant()}' already exists in this work area.");

                var deptCtrlNbr = entry.DepartmentCtrlNbr > 0 ? ControlNumber.Create(entry.DepartmentCtrlNbr) : null;
                assignment = Assignment.Create(ControlNumber.Create(entry.GroupCtrlNbr), entry.Code, entry.Name,
                    isExtra: entry.IsExtra, isActive: true, departmentCtrlNbr: deptCtrlNbr);
                uow.Assignments.Add(assignment);
                assignmentsCreated++;
            }

            if (entry.ShiftDefinitionCtrlNbr > 0 && !string.IsNullOrWhiteSpace(entry.OnDutyTime))
            {
                var onDuty = TimeOnly.Parse(entry.OnDutyTime);
                var offDuty = !string.IsNullOrWhiteSpace(entry.OffDutyTime) ? TimeOnly.Parse(entry.OffDutyTime) : onDuty.AddHours(8);
                var shiftCtrlNbr = ControlNumber.Create(entry.ShiftDefinitionCtrlNbr);

                if (entry.ExistingAssignmentCtrlNbr > 0)
                {
                    var existingSchedules = await uow.AssignmentSchedules.GetByAssignmentAsync(assignment.CtrlNbr);
                    var existingSchedule = existingSchedules.FirstOrDefault();
                    if (existingSchedule is not null)
                    {
                        if (existingSchedule.ShiftDefinitionCtrlNbr != shiftCtrlNbr)
                        {
                            uow.AssignmentSchedules.Remove(existingSchedule);
                            uow.AssignmentSchedules.Add(AssignmentSchedule.Create(assignment.CtrlNbr, shiftCtrlNbr, entry.AssignmentOperatingDaysMask, onDuty, offDuty));
                        }
                        else
                        {
                            existingSchedule.Update(entry.AssignmentOperatingDaysMask, onDuty, offDuty);
                            uow.AssignmentSchedules.Update(existingSchedule);
                        }
                        schedulesUpdated++;
                    }
                    else
                    {
                        uow.AssignmentSchedules.Add(AssignmentSchedule.Create(assignment.CtrlNbr, shiftCtrlNbr, entry.AssignmentOperatingDaysMask, onDuty, offDuty));
                        schedulesCreated++;
                    }
                }
                else
                {
                    uow.AssignmentSchedules.Add(AssignmentSchedule.Create(assignment.CtrlNbr, shiftCtrlNbr, entry.AssignmentOperatingDaysMask, onDuty, offDuty));
                    schedulesCreated++;
                }
            }

            if (existingCrewAssignmentMap.TryGetValue(assignment.CtrlNbr.Value, out var existingCa))
            {
                consumedCrewAssignmentKeys.Add(assignment.CtrlNbr.Value);
                var startUtc = !string.IsNullOrWhiteSpace(entry.StartDate)
                    ? DateTime.Parse(entry.StartDate).ToUniversalTime() : existingCa.StartUtc;
                DateTime? endUtc = !string.IsNullOrWhiteSpace(entry.EndDate)
                    ? DateTime.Parse(entry.EndDate).ToUniversalTime() : null;
                if (existingCa.DaysOfWeekMask != entry.CrewWorkDaysMask || existingCa.StartUtc != startUtc || existingCa.EndUtc != endUtc)
                {
                    existingCa.Update(entry.CrewWorkDaysMask, startUtc, endUtc);
                    uow.CrewAssignments.Update(existingCa);
                    crewAssignmentsUpdated++;
                }
                else crewAssignmentsExisting++;
            }
            else
            {
                var startUtc = !string.IsNullOrWhiteSpace(entry.StartDate)
                    ? DateTime.Parse(entry.StartDate).ToUniversalTime() : DateTime.UtcNow;
                DateTime? endUtc = !string.IsNullOrWhiteSpace(entry.EndDate)
                    ? DateTime.Parse(entry.EndDate).ToUniversalTime() : null;
                uow.CrewAssignments.Add(CrewAssignment.Create(crew.CtrlNbr, assignment.CtrlNbr, entry.CrewWorkDaysMask, startUtc, endUtc));
                crewAssignmentsCreated++;
            }
        }

        foreach (var (key, removedCa) in existingCrewAssignmentMap)
        {
            if (!consumedCrewAssignmentKeys.Contains(key))
            {
                uow.CrewAssignments.Remove(removedCa);
                crewAssignmentsDeleted++;
            }
        }

        await uow.CommitAsync(ct);

        return new WizardResult(crew.CtrlNbr.Value, crew.Name,
            assignmentsCreated, assignmentsUpdated,
            schedulesCreated, schedulesUpdated, schedulesExisting,
            crewAssignmentsCreated, crewAssignmentsUpdated, crewAssignmentsDeleted, crewAssignmentsExisting,
            positionsCreated, positionsDeleted, positionsExisting,
            IsExistingCrew: existingCrewCtrlNbr > 0);
    }

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
