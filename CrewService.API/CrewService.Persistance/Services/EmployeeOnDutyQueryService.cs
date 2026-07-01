using CrewService.Application.Employees;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
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
        var workAreaToTimeZone = await dbContext.Set<DynamicGroup>()
            .Where(g => workAreaIds.Contains(g.CtrlNbr))
            .Select(g => new { g.CtrlNbr, g.TimeZoneId })
            .ToListAsync(ct);
        var workAreaToTimeZoneMap = workAreaToTimeZone.ToDictionary(x => x.CtrlNbr, x => x.TimeZoneId);

        var result = new Dictionary<ControlNumber, EmployeeOnDutySlotDisplay>();
        foreach (var slot in slots)
        {
            string? timeZoneId = null;
            if (shiftToWorkInstanceMap.TryGetValue(slot.ShiftInstanceCtrlNbr, out var workInstanceCtrlNbr)
                && workInstanceToWorkAreaMap.TryGetValue(workInstanceCtrlNbr, out var workAreaCtrlNbr))
            {
                workAreaToTimeZoneMap.TryGetValue(workAreaCtrlNbr, out timeZoneId);
            }

            result[slot.CtrlNbr] = new EmployeeOnDutySlotDisplay(
                slot.AssignmentName,
                slot.AssignmentCode,
                slot.CrewName,
                slot.CraftRoleName,
                // The assignment's group IS its operational location; display its code (e.g. "NOYD")
                // to match the call sheet's Location column. The group is a sub-location of the work area.
                slot.GroupCode,
                timeZoneId);
        }

        return result;
    }
}
