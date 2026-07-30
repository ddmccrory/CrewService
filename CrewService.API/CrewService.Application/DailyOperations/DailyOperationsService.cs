using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public sealed class DailyOperationsService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    CallSheetVacancyProjectionSyncService vacancyProjectionSyncService)
{
    public async Task<IReadOnlyList<ShiftInstance>> GetCallSheetAsync(
        ControlNumber workAreaGroupCtrlNbr, DateOnly targetDate, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var dayStartUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEndUtc = targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var workInstances = await uow.WorkInstances.GetByWorkAreaAndDateRangeAsync(
            workAreaGroupCtrlNbr, dayStartUtc, dayEndUtc);

        if (workInstances.Count == 0)
            return [];

        return await uow.ShiftInstances.GetByWorkInstanceAsync(workInstances[0].CtrlNbr, ct);
    }

    public async Task<ShiftInstance> GetShiftInstanceAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.ShiftInstances.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {ctrlNbr} not found.");
    }

    public async Task<(ShiftInstance Shift, WorkInstance Work)> GetShiftWithWorkInstanceAsync(
        ControlNumber shiftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {shiftCtrlNbr} not found.");
        var work = await uow.WorkInstances.GetByCtrlNbrAsync(shift.WorkInstanceCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Work instance {shift.WorkInstanceCtrlNbr} not found.");
        return (shift, work);
    }

    public async Task<ShiftInstance> AnnulPositionAsync(
        ControlNumber shiftCtrlNbr, ControlNumber slotCtrlNbr, string reason, DateTime annulmentDateTime, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {shiftCtrlNbr} not found.");

        var slot = shift.PositionSlots.SingleOrDefault(s => s.CtrlNbr == slotCtrlNbr)
            ?? throw new KeyNotFoundException($"Position slot {slotCtrlNbr} not found on shift.");

        slot.Annul(reason, annulmentDateTime);
        await uow.ShiftInstances.UpdateAsync(shift, ct);
        await vacancyProjectionSyncService.ReconcileFromShiftChangeAsync(uow, shift, ct);
        await uow.CommitAsync(ct);
        return shift;
    }

    public async Task<ShiftInstance> AnnulAssignmentAsync(
        ControlNumber shiftCtrlNbr, ControlNumber assignmentCtrlNbr, string reason, DateTime annulmentDateTime, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {shiftCtrlNbr} not found.");

        var slots = shift.PositionSlots
            .Where(s => s.AssignmentCtrlNbr == assignmentCtrlNbr && s.Status != PositionSlotStatus.Annulled)
            .ToList();

        if (slots.Count == 0)
            throw new InvalidOperationException("No annullable positions found for this assignment.");

        foreach (var slot in slots)
            slot.Annul(reason, annulmentDateTime);

        await uow.ShiftInstances.UpdateAsync(shift, ct);
        await vacancyProjectionSyncService.ReconcileFromShiftChangeAsync(uow, shift, ct);
        await uow.CommitAsync(ct);
        return shift;
    }

    public async Task<ShiftInstance> DoNotFillPositionAsync(
        ControlNumber shiftCtrlNbr, ControlNumber slotCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {shiftCtrlNbr} not found.");

        var slot = shift.PositionSlots.SingleOrDefault(s => s.CtrlNbr == slotCtrlNbr)
            ?? throw new KeyNotFoundException($"Position slot {slotCtrlNbr} not found on shift.");

        slot.MarkDoNotFill();
        await uow.ShiftInstances.UpdateAsync(shift, ct);
        await vacancyProjectionSyncService.ReconcileFromShiftChangeAsync(uow, shift, ct);
        await uow.CommitAsync(ct);
        return shift;
    }

    public async Task<ShiftInstance> RestorePositionSlotAsync(
        ControlNumber shiftCtrlNbr, ControlNumber slotCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {shiftCtrlNbr} not found.");

        var slot = shift.PositionSlots.SingleOrDefault(s => s.CtrlNbr == slotCtrlNbr)
            ?? throw new KeyNotFoundException($"Position slot {slotCtrlNbr} not found on shift.");

        slot.RestoreSlot();
        await uow.ShiftInstances.UpdateAsync(shift, ct);
        await vacancyProjectionSyncService.ReconcileFromShiftChangeAsync(uow, shift, ct);
        await uow.CommitAsync(ct);
        return shift;
    }

    public async Task<ShiftInstance> RestoreAssignmentAsync(
        ControlNumber shiftCtrlNbr, ControlNumber assignmentCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {shiftCtrlNbr} not found.");

        var slots = shift.PositionSlots
            .Where(s => s.AssignmentCtrlNbr == assignmentCtrlNbr && s.IsAnnulled)
            .ToList();

        if (slots.Count == 0)
            throw new InvalidOperationException("No annulled positions found for this assignment.");

        foreach (var slot in slots)
            slot.RestoreSlot();

        await uow.ShiftInstances.UpdateAsync(shift, ct);
        await vacancyProjectionSyncService.ReconcileFromShiftChangeAsync(uow, shift, ct);
        await uow.CommitAsync(ct);
        return shift;
    }

    public async Task<ShiftInstance> SaveAssignmentNoteAsync(
        ControlNumber shiftCtrlNbr, ControlNumber assignmentCtrlNbr, string noteText, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {shiftCtrlNbr} not found.");

        shift.SetAssignmentNote(assignmentCtrlNbr, noteText);
        await uow.ShiftInstances.UpdateAsync(shift, ct);
        await uow.CommitAsync(ct);
        return shift;
    }

    public async Task<ShiftInstance> ManageAssignmentPositionsAsync(
        ControlNumber shiftCtrlNbr, ControlNumber assignmentCtrlNbr,
        IEnumerable<ControlNumber> removedSlotCtrlNbrs,
        IEnumerable<string> addedCraftRoleNames,
        IEnumerable<(ControlNumber CtrlNbr, int DisplayOrder)> positionSlotOrders,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {shiftCtrlNbr} not found.");

        foreach (var slotCtrlNbr in removedSlotCtrlNbrs)
            shift.RemovePositionSlot(slotCtrlNbr);

        foreach (var craftRoleName in addedCraftRoleNames)
            shift.AddAdHocPositionSlot(assignmentCtrlNbr, craftRoleName);

        var orders = positionSlotOrders.ToList();
        if (orders.Count > 0)
            shift.ReorderPositionSlots(orders.Select(o => (o.CtrlNbr, o.DisplayOrder)));

        await uow.ShiftInstances.UpdateAsync(shift, ct);
        await vacancyProjectionSyncService.ReconcileFromShiftChangeAsync(uow, shift, ct);
        await uow.CommitAsync(ct);
        return shift;
    }

    public async Task CloseShiftInstanceAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {ctrlNbr} not found.");
        shift.Complete();
        await uow.ShiftInstances.UpdateAsync(shift, ct);
        await uow.CommitAsync(ct);
    }

    public async Task<ShiftInstance> ReopenShiftInstanceAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {ctrlNbr} not found.");
        shift.Reopen();
        await uow.ShiftInstances.UpdateAsync(shift, ct);
        await uow.CommitAsync(ct);
        return shift;
    }

    public async Task<ShiftInstance> AddAssignmentFromTemplateAsync(
        ControlNumber shiftCtrlNbr, ControlNumber assignmentCtrlNbr,
        IAssignmentQueryService assignmentQuery,
        string? onDutyTimeOverride, string? offDutyTimeOverride,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {shiftCtrlNbr} not found.");

        var workInstance = await uow.WorkInstances.GetByCtrlNbrAsync(shift.WorkInstanceCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Work instance {shift.WorkInstanceCtrlNbr} not found.");

        var targetDate = DateOnly.FromDateTime(workInstance.StartUtc);
        var extras = await assignmentQuery.GetExtraAssignmentsForShiftAsync(
            workInstance.WorkAreaGroupCtrlNbr, shift.ShiftDefinitionCtrlNbr, targetDate, shift.DepartmentCtrlNbr, ct);

        var template = extras.FirstOrDefault(a => a.AssignmentCtrlNbr == assignmentCtrlNbr)
            ?? throw new KeyNotFoundException($"Extra assignment {assignmentCtrlNbr} not found or not eligible.");

        var onDutyTime = !string.IsNullOrEmpty(onDutyTimeOverride) && TimeOnly.TryParse(onDutyTimeOverride, out var parsedOn)
            ? parsedOn : template.OnDutyTime;
        var offDutyTime = !string.IsNullOrEmpty(offDutyTimeOverride) && TimeOnly.TryParse(offDutyTimeOverride, out var parsedOff)
            ? parsedOff : template.OffDutyTime;

        var positions = template.Positions
            .Select(p => (p.PositionCtrlNbr, p.IncumbentEmployeeCtrlNbr, p.DisplayOrder, p.CraftRoleName, p.CrewName, p.CrewType))
            .ToList();

        shift.AddTemplateAssignment(
            template.AssignmentCtrlNbr, template.AssignmentCode, template.AssignmentName,
            template.GroupName, template.GroupCode, onDutyTime, offDutyTime, positions);

        await uow.ShiftInstances.UpdateAsync(shift, ct);
        await vacancyProjectionSyncService.ReconcileFromShiftChangeAsync(uow, shift, ct);
        await uow.CommitAsync(ct);
        return shift;
    }

    public async Task<ShiftInstance> AddAdHocAssignmentAsync(
        ControlNumber shiftCtrlNbr, string assignmentCode, string assignmentName,
        string groupName, string groupCode, TimeOnly onDutyTime, TimeOnly offDutyTime,
        IReadOnlyList<string> craftRoleNames, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {shiftCtrlNbr} not found.");

        shift.AddAdHocAssignment(assignmentCode, assignmentName, groupName, groupCode, onDutyTime, offDutyTime, craftRoleNames);
        await uow.ShiftInstances.UpdateAsync(shift, ct);
        await vacancyProjectionSyncService.ReconcileFromShiftChangeAsync(uow, shift, ct);
        await uow.CommitAsync(ct);
        return shift;
    }

    public async Task<ShiftInstance> RemoveAssignmentAsync(
        ControlNumber shiftCtrlNbr, ControlNumber assignmentCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {shiftCtrlNbr} not found.");

        shift.RemoveAssignment(assignmentCtrlNbr);
        await uow.ShiftInstances.UpdateAsync(shift, ct);
        await vacancyProjectionSyncService.ReconcileFromShiftChangeAsync(uow, shift, ct);
        await uow.CommitAsync(ct);
        return shift;
    }

    public async Task<(IReadOnlyList<AssignmentDto> Extras, HashSet<ControlNumber> ExistingAssignmentCtrlNbrs)>
        GetAvailableExtraAssignmentsAsync(
            ControlNumber shiftCtrlNbr,
            IAssignmentQueryService assignmentQuery,
            CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {shiftCtrlNbr} not found.");

        var workInstance = await uow.WorkInstances.GetByCtrlNbrAsync(shift.WorkInstanceCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Work instance {shift.WorkInstanceCtrlNbr} not found.");

        var targetDate = DateOnly.FromDateTime(workInstance.StartUtc);
        var extras = await assignmentQuery.GetExtraAssignmentsForShiftAsync(
            workInstance.WorkAreaGroupCtrlNbr, shift.ShiftDefinitionCtrlNbr, targetDate, shift.DepartmentCtrlNbr, ct);

        var existing = shift.PositionSlots
            .Select(s => s.AssignmentCtrlNbr)
            .Distinct()
            .ToHashSet();

        return (extras, existing);
    }
}
