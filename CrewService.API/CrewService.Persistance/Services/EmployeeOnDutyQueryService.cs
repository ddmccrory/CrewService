using CrewService.Application.Employees;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Services;

/// <summary>
/// Resolves denormalized display data and the work-area timezone for on-duty position slots.
/// Reads the <c>PositionSlotInstance</c> rows directly (the entity has a mapped table but no
/// aggregate-external repository) and walks
/// <c>PositionSlotInstance → ShiftInstance → WorkInstance → DynamicGroup</c> to find each slot's
/// work-area timezone id.
/// </summary>
internal sealed class EmployeeOnDutyQueryService(CrewServiceDbContext dbContext) : IEmployeeOnDutyQueryService
{
    public async Task<IReadOnlyDictionary<ControlNumber, EmployeeOnDutySlotDisplay>> GetSlotDisplayAsync(
        IReadOnlyList<ControlNumber> positionSlotCtrlNbrs, CancellationToken ct = default)
    {
        if (positionSlotCtrlNbrs.Count == 0)
            return new Dictionary<ControlNumber, EmployeeOnDutySlotDisplay>();

        var slotIds = positionSlotCtrlNbrs.Distinct().ToList();

        var slots = await dbContext.Set<PositionSlotInstance>()
            .Where(s => slotIds.Contains(s.CtrlNbr))
            .ToListAsync(ct);

        if (slots.Count == 0)
            return new Dictionary<ControlNumber, EmployeeOnDutySlotDisplay>();

        // Resolve each slot's work-area timezone: slot → shift → work-instance → work-area group.
        var shiftIds = slots.Select(s => s.ShiftInstanceCtrlNbr).Distinct().ToList();
        var shiftToWorkInstance = await dbContext.Set<ShiftInstance>()
            .Where(s => shiftIds.Contains(s.CtrlNbr))
            .Select(s => new { s.CtrlNbr, s.WorkInstanceCtrlNbr })
            .ToListAsync(ct);
        var shiftToWorkInstanceMap = shiftToWorkInstance.ToDictionary(x => x.CtrlNbr, x => x.WorkInstanceCtrlNbr);

        var workInstanceIds = shiftToWorkInstance.Select(x => x.WorkInstanceCtrlNbr).Distinct().ToList();
        var workInstanceToWorkArea = await dbContext.Set<WorkInstance>()
            .Where(w => workInstanceIds.Contains(w.CtrlNbr))
            .Select(w => new { w.CtrlNbr, w.WorkAreaGroupCtrlNbr })
            .ToListAsync(ct);
        var workInstanceToWorkAreaMap = workInstanceToWorkArea.ToDictionary(x => x.CtrlNbr, x => x.WorkAreaGroupCtrlNbr);

        var workAreaIds = workInstanceToWorkArea.Select(x => x.WorkAreaGroupCtrlNbr).Distinct().ToList();
        var workAreas = await dbContext.Set<DynamicGroup>()
            .Where(g => workAreaIds.Contains(g.CtrlNbr))
            .Select(g => new { g.CtrlNbr, g.TimeZoneId, Name = g.Name ?? string.Empty, Code = g.Code ?? string.Empty })
            .ToListAsync(ct);
        var workAreaToTimeZoneMap = workAreas.ToDictionary(x => x.CtrlNbr, x => x.TimeZoneId);
        var workAreaNameMap = workAreas.ToDictionary(x => x.CtrlNbr, x => x.Name);
        var workAreaCodeMap = workAreas.ToDictionary(x => x.CtrlNbr, x => x.Code ?? string.Empty);

        var crewPositionIds = slots
            .Where(s => s.CrewPositionCtrlNbr is not null)
            .Select(s => s.CrewPositionCtrlNbr!)
            .Distinct()
            .ToList();

        var crewPositionCraftRoleMap = crewPositionIds.Count == 0
            ? new Dictionary<ControlNumber, ControlNumber>()
            : await dbContext.Set<CrewPosition>()
                .Where(cp => crewPositionIds.Contains(cp.CtrlNbr))
                .Select(cp => new { cp.CtrlNbr, cp.CraftRoleCtrlNbr })
                .ToDictionaryAsync(x => x.CtrlNbr, x => x.CraftRoleCtrlNbr, ct);

        var craftRoleIds = crewPositionCraftRoleMap.Values.Distinct().ToList();
        var craftRoleToCraftMap = craftRoleIds.Count == 0
            ? new Dictionary<ControlNumber, long>()
            : await dbContext.Set<CraftRole>()
                .Where(cr => craftRoleIds.Contains(cr.CtrlNbr))
                .Select(cr => new { cr.CtrlNbr, CraftCtrlNbr = cr.CraftCtrlNbr.Value })
                .ToDictionaryAsync(x => x.CtrlNbr, x => x.CraftCtrlNbr, ct);

        var result = new Dictionary<ControlNumber, EmployeeOnDutySlotDisplay>();
        foreach (var slot in slots)
        {
            string? timeZoneId = null;
            long? workAreaCtrlNbr = null;
            var workAreaName = string.Empty;
            var workAreaCode = string.Empty;
            var craftCtrlNbr = 0L;
            if (shiftToWorkInstanceMap.TryGetValue(slot.ShiftInstanceCtrlNbr, out var workInstanceCtrlNbr)
                && workInstanceToWorkAreaMap.TryGetValue(workInstanceCtrlNbr, out var slotWorkAreaCtrlNbr))
            {
                workAreaCtrlNbr = slotWorkAreaCtrlNbr.Value;
                workAreaToTimeZoneMap.TryGetValue(slotWorkAreaCtrlNbr, out timeZoneId);
                workAreaCodeMap.TryGetValue(slotWorkAreaCtrlNbr, out workAreaCode);
                if (!workAreaNameMap.TryGetValue(slotWorkAreaCtrlNbr, out var resolvedWorkAreaName)
                    || string.IsNullOrWhiteSpace(resolvedWorkAreaName))
                {
                    resolvedWorkAreaName = string.Empty;
                }

                workAreaName = resolvedWorkAreaName;
            }

            if (slot.CrewPositionCtrlNbr is not null
                && crewPositionCraftRoleMap.TryGetValue(slot.CrewPositionCtrlNbr, out var craftRoleCtrlNbr)
                && craftRoleToCraftMap.TryGetValue(craftRoleCtrlNbr, out var resolvedCraftCtrlNbr))
            {
                craftCtrlNbr = resolvedCraftCtrlNbr;
            }

            result[slot.CtrlNbr] = new EmployeeOnDutySlotDisplay(
                slot.ShiftInstanceCtrlNbr.Value,
                slot.AssignmentName,
                slot.AssignmentCode,
                slot.CrewName,
                slot.CraftRoleName,
                craftCtrlNbr,
                // The assignment's group IS its operational location; display its code (e.g. "NOYD")
                // to match the call sheet's Location column. The group is a sub-location of the work area.
                slot.GroupCode,
                workAreaCtrlNbr,
                workAreaName,
                workAreaCode,
                timeZoneId,
                slot.OffDutyTime);
        }

        return result;
    }
}
