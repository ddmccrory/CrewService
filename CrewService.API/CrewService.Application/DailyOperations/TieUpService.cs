using CrewService.Domain.Interfaces;
using CrewService.Application.TenantConfig;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Application.FraCompliance;
using CrewService.Application.Notifications;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public sealed class TieUpService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    FraRestValidator fraRestValidator,
    EmployeeNotificationService notifications,
    IRailroadResolver railroadResolver,
    ICurrentUserService currentUserService)
{
    public async Task<OffDutyRecord> ExecuteAsync(
        ControlNumber onDutyRecordCtrlNbr,
        DateTime offDutyTimeUtc,
        string releaseReason,
        ControlNumber craftCtrlNbr,
        bool offDutyTimeConfirmed,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var onDutyRecord = await uow.OnDutyRecords.GetByCtrlNbrAsync(onDutyRecordCtrlNbr, ct)
            ?? throw new InvalidOperationException("On-duty record not found");

        if (offDutyTimeUtc < onDutyRecord.OnDutyTimeUtc)
            throw new InvalidOperationException("Off-duty time cannot be earlier than on-duty time.");

        var policy = await uow.CraftOperationsPolicies.GetByCraftAsync(craftCtrlNbr, ct);
        var craft = await uow.Crafts.GetByCtrlNbrAsync(craftCtrlNbr, ct);
        var standard = await ResolveRegulatoryStandardAsync(uow, craft, ct);

        var totalMinutes = (int)(offDutyTimeUtc - onDutyRecord.OnDutyTimeUtc).TotalMinutes;
        var restHours = CalculateRestHours(policy, totalMinutes);
        var consecutiveDayResetHours = policy?.ConsecutiveDayResetHours ?? 24m;
        var isQuickTieUp = standard is not null && fraRestValidator.IsQuickTieUp(standard, totalMinutes);

        await UpsertFraDutyTourAsync(uow, onDutyRecord, offDutyTimeUtc, totalMinutes, standard, isQuickTieUp, ct);

        var offDutyRecord = OffDutyRecord.Create(
            onDutyRecordCtrlNbr,
            onDutyRecord.EmployeeCtrlNbr,
            offDutyTimeUtc,
            totalMinutes,
            restHours,
            consecutiveDayResetHours,
            releaseReason,
            offDutyTimeConfirmed: offDutyTimeConfirmed,
            offDutyTimeConfirmedAtUtc: offDutyTimeConfirmed ? DateTime.UtcNow : null,
            offDutyTimeConfirmedBy: offDutyTimeConfirmed ? currentUserService.GetUserName() : null);

        onDutyRecord.TieUp(requiresDeferredEmployeeCompletion: isQuickTieUp || !offDutyTimeConfirmed);

        var tieUpContext = await uow.OnDutyRecords.GetTieUpContextAsync(onDutyRecord.CtrlNbr, ct);

        var shift = tieUpContext is null
            ? null
            : await uow.ShiftInstances.GetByCtrlNbrAsync(tieUpContext.ShiftInstanceCtrlNbr, ct);
        if (shift is not null)
        {
            var slot = shift.PositionSlots.SingleOrDefault(s => s.CtrlNbr == onDutyRecord.PositionSlotCtrlNbr);
            if (slot is not null)
            {
                slot.MarkTiedUp();

                var boardSlot = shift.BoardSlots
                    .Where(b => b.EmployeeCtrlNbr == onDutyRecord.EmployeeCtrlNbr)
                    .OrderByDescending(b => b.CallSequence)
                    .ThenBy(b => b.CtrlNbr.Value)
                    .FirstOrDefault();

                if (boardSlot is not null)
                {
                    boardSlot.MarkTiedUp(boardSlot.CallSequence);
                    boardSlot.UpdateOperationalTracking(
                        boardSlot.DaysWorked,
                        onDutyRecord.ConsecutiveDays,
                        offDutyRecord.TwentyFourHourRestAtUtc);
                }

                uow.ShiftInstances.Update(shift);
            }
        }

        await ApplyBoardOrderTieUpAdjustmentAsync(
            uow,
            onDutyRecord.EmployeeCtrlNbr,
            craftCtrlNbr,
            offDutyTimeUtc,
            ct);

        await uow.OffDutyRecords.AddAsync(offDutyRecord, ct);

        if (isQuickTieUp && tieUpContext is not null)
        {
            var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(tieUpContext.WorkAreaCtrlNbr, ct);
            var railroadCtrlNbr = railroadResolver.ResolveFromGroup(workArea);
            if (workArea is not null && railroadCtrlNbr is not null)
            {
                await notifications.NotifyTieUpOutstandingAsync(
                    uow,
                    railroadCtrlNbr,
                    workArea.CtrlNbr,
                    onDutyRecord.EmployeeCtrlNbr,
                    tieUpContext.AssignmentCode,
                    onDutyRecord.OnDutyTimeUtc,
                    ct);
            }
        }

        if (tieUpContext is not null)
        {
            await AutoCloseShiftIfAllOnDutyStartedAsync(uow, tieUpContext.ShiftInstanceCtrlNbr, ct);
        }

        await uow.CommitAsync(ct);

        return offDutyRecord;
    }

    public async Task AutoCloseShiftIfAllOnDutyStartedAsync(
        ControlNumber shiftInstanceCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var changed = await AutoCloseShiftIfAllOnDutyStartedAsync(uow, shiftInstanceCtrlNbr, ct);
        if (changed)
            await uow.CommitAsync(ct);
    }

    private static async Task<bool> AutoCloseShiftIfAllOnDutyStartedAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber shiftInstanceCtrlNbr,
        CancellationToken ct = default)
    {

        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftInstanceCtrlNbr, ct);
        if (shift is null || shift.IsComplete)
            return false;

        var completionStatuses = await uow.OnDutyRecords.GetCompletionStatusesForShiftAsync(shiftInstanceCtrlNbr, ct);
        if (completionStatuses.Count == 0)
            return false;

        if (completionStatuses.All(status => status != OnDutyCompletionStatus.NotStarted))
        {
            shift.Complete();
            await uow.ShiftInstances.UpdateAsync(shift, ct);
            return true;
        }

        return false;
    }

    private static async Task<RegulatoryStandard?> ResolveRegulatoryStandardAsync(
        IOrchestrationUnitOfWork uow,
        Craft? craft,
        CancellationToken ct)
    {
        if (craft?.RegulatoryStandardCtrlNbr is not null)
            return await uow.RegulatoryStandards.GetByCtrlNbrAsync(craft.RegulatoryStandardCtrlNbr, ct);

        var all = await uow.RegulatoryStandards.GetAllAsync(ct);
        return all.OrderByDescending(s => s.EffectiveDate).FirstOrDefault();
    }

    private static async Task UpsertFraDutyTourAsync(
        IOrchestrationUnitOfWork uow,
        OnDutyRecord onDutyRecord,
        DateTime offDutyTimeUtc,
        int totalMinutes,
        RegulatoryStandard? standard,
        bool isQuickTieUp,
        CancellationToken ct)
    {
        if (standard is null)
            return;

        var active = await uow.FraDutyTours.GetActiveTourForEmployeeAsync(onDutyRecord.EmployeeCtrlNbr, ct);
        if (active is null)
        {
            var lastOffDuty = await uow.OffDutyRecords.GetLastForEmployeeAsync(onDutyRecord.EmployeeCtrlNbr, ct);
            var priorMinutes = lastOffDuty is null
                ? int.MaxValue
                : (int)(onDutyRecord.OnDutyTimeUtc - lastOffDuty.OffDutyTimeUtc).TotalMinutes;

            active = FraDutyTour.Create(
                onDutyRecord.EmployeeCtrlNbr,
                standard.CtrlNbr,
                onDutyRecord.OnDutyTimeUtc,
                priorMinutes,
                onDutyRecord.ConsecutiveDays);

            await uow.FraDutyTours.AddAsync(active, ct);
        }

        var excessMinutes = Math.Max(0, totalMinutes - standard.MaxOnDutyMinutes);
        active.Close(
            offDutyTimeUtc,
            totalMinutes,
            excessMinutes > 0 ? excessMinutes : null,
            excessMinutes > 0 ? "Exceeded maximum on-duty minutes" : null,
            isQuickTieUp);
    }

    private static decimal CalculateRestHours(CraftOperationsPolicy? policy, int totalMinutes)
    {
        if (policy is null) return 10m;

        return policy.RestCalculationStrategy switch
        {
            "FixedHours" => policy.FixedRestHours ?? 10m,
            "CraftConfigured" => CalculateCraftConfiguredRest(totalMinutes),
            _ => 10m // "FRA" — actual FRA rest calc handled by FraCompliance module
        };
    }

    private static decimal CalculateCraftConfiguredRest(int totalMinutes)
    {
        var baseRest = 10m;
        var excessMinutes = Math.Max(0, totalMinutes - 720);
        var penalty = excessMinutes > 0 ? Math.Ceiling(excessMinutes / 60m) : 0;
        return baseRest + penalty;
    }

    private static async Task ApplyBoardOrderTieUpAdjustmentAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        ControlNumber craftCtrlNbr,
        DateTime actualOffDutyTimeUtc,
        CancellationToken ct)
    {
        var employeeAssignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
        if (employeeAssignments.Count == 0)
            return;

        var staffablePositionIds = employeeAssignments
            .Select(a => a.StaffablePositionCtrlNbr)
            .ToHashSet();

        var activeBoards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(craftCtrlNbr, ct);
        var candidateBoards = activeBoards
            .Where(b => b.IsActive)
            .Where(b => b.Positions.Any(p => staffablePositionIds.Contains(p.StaffablePositionCtrlNbr)))
            .ToList();

        if (candidateBoards.Count == 0)
            return;

        var selectedBoard = candidateBoards
            .Where(b => b.Positions.Any(p => p.EmployeeCtrlNbr == employeeCtrlNbr))
            .OrderBy(b => b.CtrlNbr.Value)
            .FirstOrDefault();

        if (selectedBoard is null)
            return;

        var selectedPosition = selectedBoard.Positions.FirstOrDefault(p => p.EmployeeCtrlNbr == employeeCtrlNbr);
        if (selectedPosition is null)
            return;

        selectedPosition.SetTieUpOrderUtcIfLater(actualOffDutyTimeUtc);

        var ordering = selectedBoard.Positions
            .OrderBy(p => p.TieUpOrderUtc ?? DateTime.MinValue)
            .ThenBy(p => p.OrderSeedBoardPosition)
            .ThenBy(p => p.PositionOrder)
            .ThenBy(p => p.CtrlNbr.Value)
            .Select((p, index) => (p.CtrlNbr, index + 1))
            .ToList();

        selectedBoard.ReorderPositions(ordering);
        await uow.RosterBoards.UpdateAsync(selectedBoard, ct);
    }
}

