using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Application.BackgroundWorkers;
using CrewService.Application.DailyOperations;

namespace CrewService.Application.WorkManagement;

public sealed class WorkManagementService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    IDailyCallSheetSchedulerService dailyCallSheetScheduler,
    IDailyCallSheetScheduleSignal dailyCallSheetScheduleSignal)
{
    // ── Work Instances ───────────────────────────────────────────────────────

    public async Task<List<WorkInstance>> GetWorkInstancesAsync(
        ControlNumber workAreaGroupCtrlNbr, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.WorkInstances.GetByWorkAreaAndDateRangeAsync(workAreaGroupCtrlNbr, startUtc, endUtc);
    }

    public async Task<WorkInstance> CreateWorkInstanceAsync(
        long? assignmentGroupCtrlNbr, long workAreaGroupCtrlNbr,
        DateTime startUtc, DateTime endUtc, DateTime? callTimeUtc, CancellationToken ct = default)
    {
        var instance = WorkInstance.Create(assignmentGroupCtrlNbr, workAreaGroupCtrlNbr, startUtc, endUtc, callTimeUtc);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.WorkInstances.Add(instance);
        await uow.CommitAsync(ct);
        return instance;
    }

    // ── Position Slots ───────────────────────────────────────────────────────

    public async Task<List<PositionSlot>> GetPositionSlotsAsync(ControlNumber workInstanceCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PositionSlots.GetByWorkInstanceAsync(workInstanceCtrlNbr);
    }

    public async Task<PositionSlot> CreatePositionSlotAsync(
        long workInstanceCtrlNbr, long craftRoleCtrlNbr, CancellationToken ct = default)
    {
        var slot = PositionSlot.Create(workInstanceCtrlNbr, craftRoleCtrlNbr);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.PositionSlots.Add(slot);
        await uow.CommitAsync(ct);
        return slot;
    }

    public async Task<PositionSlot> BindSlotAsync(
        ControlNumber ctrlNbr, long employeeCtrlNbr, string source, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var slot = await uow.PositionSlots.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Slot {ctrlNbr.Value} not found.");
        slot.Bind(employeeCtrlNbr, source);
        uow.PositionSlots.Update(slot);
        await uow.CommitAsync(ct);
        return slot;
    }

    public async Task<PositionSlot> UnbindSlotAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var slot = await uow.PositionSlots.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Slot {ctrlNbr.Value} not found.");
        slot.Unbind();
        uow.PositionSlots.Update(slot);
        await uow.CommitAsync(ct);
        return slot;
    }

    // ── Craft Roles ──────────────────────────────────────────────────────────

    public async Task<List<CraftRole>> GetCraftRolesAsync(
        ControlNumber? departmentCtrlNbr, ControlNumber? craftCtrlNbr, ControlNumber? railroadCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        if (departmentCtrlNbr is not null)
            return await uow.CraftRoles.GetByDepartmentAsync(departmentCtrlNbr);
        if (craftCtrlNbr is not null)
            return await uow.CraftRoles.GetByCraftAsync(craftCtrlNbr);
        if (railroadCtrlNbr is not null)
            return await uow.CraftRoles.GetByRailroadAsync(railroadCtrlNbr);
        return await uow.CraftRoles.GetAllAsync(ct);
    }

    public async Task<CraftRole> CreateCraftRoleAsync(
        long craftCtrlNbr, string code, string name, string alternateName, CancellationToken ct = default)
    {
        var role = CraftRole.Create(craftCtrlNbr, code, name, alternateName);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.CraftRoles.Add(role);
        await uow.CommitAsync(ct);
        return role;
    }

    public async Task<CraftRole> UpdateCraftRoleAsync(
        ControlNumber ctrlNbr, string code, string name, string alternateName, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var role = await uow.CraftRoles.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"CraftRole {ctrlNbr.Value} not found.");
        role.Update(code, name, alternateName);
        uow.CraftRoles.Update(role);
        await uow.CommitAsync(ct);
        return role;
    }

    public async Task DeleteCraftRoleAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var role = await uow.CraftRoles.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"CraftRole {ctrlNbr.Value} not found.");
        uow.CraftRoles.Remove(role);
        await uow.CommitAsync(ct);
    }

    // ── Craft Role Qualifications ────────────────────────────────────────────

    public async Task<List<CraftRoleQualification>> GetCraftRoleQualificationsAsync(
        ControlNumber craftRoleCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.CraftRoleQualifications.GetByCraftRoleAsync(craftRoleCtrlNbr);
    }

    public async Task<CraftRoleQualification> AddCraftRoleQualificationAsync(
        ControlNumber craftRoleCtrlNbr, ControlNumber qualificationTypeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var role = await uow.CraftRoles.GetByCtrlNbrWithQualificationsAsync(craftRoleCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"CraftRole {craftRoleCtrlNbr.Value} not found.");
        var qualType = await uow.QualificationTypes.GetByCtrlNbrAsync(qualificationTypeCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"QualificationType {qualificationTypeCtrlNbr.Value} not found.");

        if (qualType.CraftCtrlNbr is not null && qualType.CraftCtrlNbr != role.CraftCtrlNbr)
            throw new InvalidOperationException(
                $"Qualification '{qualType.Name}' is restricted to a different craft and cannot be assigned to this role.");

        var rq = role.AddRequiredQualification(qualificationTypeCtrlNbr);
        uow.CraftRoleQualifications.Add(rq);
        uow.CraftRoles.Update(role);
        await uow.CommitAsync(ct);
        return rq;
    }

    public async Task RemoveCraftRoleQualificationAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var rq = await uow.CraftRoleQualifications.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"CraftRoleQualification {ctrlNbr.Value} not found.");
        var role = await uow.CraftRoles.GetByCtrlNbrWithQualificationsAsync(rq.CraftRoleCtrlNbr, ct)
            ?? throw new KeyNotFoundException("CraftRole not found.");
        role.RemoveRequiredQualification(ctrlNbr);
        uow.CraftRoleQualifications.Remove(rq);
        uow.CraftRoles.Update(role);
        await uow.CommitAsync(ct);
    }

    // ── Shift Definitions ────────────────────────────────────────────────────

    public async Task<List<ShiftDefinition>> GetShiftDefinitionsAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.ShiftDefinitions.GetByWorkAreaAsync(workAreaGroupCtrlNbr);
    }

    public async Task<ShiftDefinition> CreateShiftDefinitionAsync(
        ControlNumber workAreaGroupCtrlNbr, string shiftCode, string displayName,
        int displayOrder, bool isActive, CancellationToken ct = default)
    {
        var shift = ShiftDefinition.Create(workAreaGroupCtrlNbr, shiftCode, displayName, displayOrder, isActive);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.ShiftDefinitions.Add(shift);
        await uow.CommitAsync(ct);

        await NotifyNextCallSheetEventAsync(workAreaGroupCtrlNbr, ct);

        return shift;
    }

    public async Task<ShiftDefinition> UpdateShiftDefinitionAsync(
        ControlNumber ctrlNbr, string shiftCode, string displayName,
        int displayOrder, bool isActive, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftDefinitions.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"ShiftDefinition {ctrlNbr.Value} not found.");
        var workAreaCtrlNbr = shift.WorkAreaGroupCtrlNbr;
        shift.Update(shiftCode: shiftCode, displayName: displayName, displayOrder: displayOrder, isActive: isActive);
        uow.ShiftDefinitions.Update(shift);
        await uow.CommitAsync(ct);

        await NotifyNextCallSheetEventAsync(workAreaCtrlNbr, ct);

        return shift;
    }

    public async Task DeleteShiftDefinitionAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftDefinitions.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"ShiftDefinition {ctrlNbr.Value} not found.");
        var workAreaCtrlNbr = shift.WorkAreaGroupCtrlNbr;
        uow.ShiftDefinitions.Remove(shift);
        await uow.CommitAsync(ct);

        await NotifyNextCallSheetEventAsync(workAreaCtrlNbr, ct);
    }

    private async Task NotifyNextCallSheetEventAsync(ControlNumber workAreaCtrlNbr, CancellationToken ct)
    {
        var nextEvent = await dailyCallSheetScheduler.GetNextCallSheetEventUtcAsync(workAreaCtrlNbr, ct);
        if (nextEvent.HasValue)
            dailyCallSheetScheduleSignal.Notify(nextEvent.Value);
    }
}
