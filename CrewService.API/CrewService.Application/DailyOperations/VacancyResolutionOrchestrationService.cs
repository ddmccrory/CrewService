using CrewService.Application.Workflows;
using CrewService.Application.Workflows.Effects;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public sealed class VacancyResolutionOrchestrationService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    WorkflowRuntimeService workflowRuntimeService,
    OnDutyPlacementService onDutyPlacementService,
    CallSheetVacancyProjectionSyncService vacancyProjectionSyncService)
{
    public async Task<IReadOnlyList<VacancyFillCandidateDto>> GetFillCandidatesAsync(
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber shiftInstanceCtrlNbr,
        ControlNumber positionSlotCtrlNbr,
        ControlNumber? craftCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(shiftInstanceCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Shift instance {shiftInstanceCtrlNbr.Value} not found.");

        var slot = shift.PositionSlots.SingleOrDefault(s => s.CtrlNbr == positionSlotCtrlNbr)
            ?? throw new KeyNotFoundException($"Position slot {positionSlotCtrlNbr.Value} not found on shift.");

        var resolvedCraftCtrlNbr = craftCtrlNbr ?? await ResolveCraftCtrlNbrAsync(uow, slot, ct);
        var boardRows = await GetCandidateBoardRowsAsync(uow, workAreaGroupCtrlNbr, shift, resolvedCraftCtrlNbr, ct);
        var activeBoards = await uow.RosterBoards.GetActiveByWorkAreaAsync(workAreaGroupCtrlNbr, ct);
        var boardTypeByCtrlNbr = activeBoards.ToDictionary(b => b.CtrlNbr, b => b.BoardType);
        var projectedEmployeeCtrlNbr = await GetProjectedEmployeeCtrlNbrAsync(uow, slot.CtrlNbr);

        var employeesByCtrlNbr = new Dictionary<ControlNumber, Domain.Models.Employees.Employee>();
        foreach (var employeeCtrlNbr in boardRows.Select(b => b.EmployeeCtrlNbr).Distinct())
        {
            var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct);
            if (employee is not null)
                employeesByCtrlNbr[employeeCtrlNbr] = employee;
        }

        if (projectedEmployeeCtrlNbr is not null && !employeesByCtrlNbr.ContainsKey(projectedEmployeeCtrlNbr))
        {
            var projectedEmployee = await uow.Employees.GetByCtrlNbrAsync(projectedEmployeeCtrlNbr, ct);
            if (projectedEmployee is not null)
                employeesByCtrlNbr[projectedEmployeeCtrlNbr] = projectedEmployee;
        }

        var candidates = new List<VacancyFillCandidateDto>();
        if (projectedEmployeeCtrlNbr is not null
            && employeesByCtrlNbr.TryGetValue(projectedEmployeeCtrlNbr, out var projectedEmployeeInfo))
        {
            var projectedBoardRow = boardRows.FirstOrDefault(r => r.EmployeeCtrlNbr == projectedEmployeeCtrlNbr);
            var projectedContacts = BuildContacts(projectedEmployeeInfo);

            var projectedBoardType = projectedBoardRow is not null
                && boardTypeByCtrlNbr.TryGetValue(projectedBoardRow.RosterBoardCtrlNbr, out var matchedBoardType)
                    ? matchedBoardType.ToString()
                    : BoardType.ExtraBoard.ToString();

            candidates.Add(new VacancyFillCandidateDto(
                projectedEmployeeCtrlNbr,
                projectedEmployeeInfo.EmployeeNumber,
                projectedBoardRow?.EmployeeName
                    ?? (!string.IsNullOrWhiteSpace(projectedEmployeeInfo.EmployeeNumber)
                        ? projectedEmployeeInfo.EmployeeNumber
                        : $"Emp #{projectedEmployeeCtrlNbr.Value}"),
                projectedBoardType,
                projectedBoardRow?.BoardOrder ?? 0,
                projectedBoardRow?.CallSequence ?? 0,
                QualificationStatus: "Projected",
                StatusDisplay: "Projected",
                ProjectedVacancyDisplay: projectedBoardRow?.PositionName ?? slot.AssignmentName,
                OnDutyDisplay: string.Empty,
                projectedContacts,
                projectedBoardRow?.CtrlNbr ?? ControlNumber.Create(0)));
        }

        candidates.AddRange(boardRows
            .Where(row => projectedEmployeeCtrlNbr is null || row.EmployeeCtrlNbr != projectedEmployeeCtrlNbr)
            .Select(row =>
            {
                employeesByCtrlNbr.TryGetValue(row.EmployeeCtrlNbr, out var employee);
                var contacts = BuildContacts(employee);

                return new VacancyFillCandidateDto(
                    row.EmployeeCtrlNbr,
                    employee?.EmployeeNumber ?? string.Empty,
                    row.EmployeeName,
                    boardTypeByCtrlNbr.TryGetValue(row.RosterBoardCtrlNbr, out var boardType)
                        ? boardType.ToString()
                        : BoardType.ExtraBoard.ToString(),
                    row.BoardOrder,
                    row.CallSequence,
                    QualificationStatus: "Eligible",
                    StatusDisplay: row.Status.ToString(),
                    ProjectedVacancyDisplay: row.PositionName,
                    OnDutyDisplay: string.Empty,
                    contacts,
                    row.CtrlNbr);
            })
            .ToList());

        return candidates;
    }

    public async Task<VacancyFillResult> FillVacancyAsync(
        VacancyFillRequest request,
        CancellationToken ct = default)
    {
        var normalizedForceReason = string.IsNullOrWhiteSpace(request.ForceReason)
            ? null
            : request.ForceReason.Trim();

        var normalizedDispatcherNote = string.IsNullOrWhiteSpace(request.DispatcherNote)
            ? null
            : request.DispatcherNote.Trim();

        var normalizedLateCallNote = string.IsNullOrWhiteSpace(request.LateCallNote)
            ? null
            : request.LateCallNote.Trim();

        var normalizedArrivalFollowUpNote = string.IsNullOrWhiteSpace(request.ArrivalFollowUpNote)
            ? null
            : request.ArrivalFollowUpNote.Trim();

        ControlNumber? resolvedCraftCtrlNbr;
        ControlNumber railroadCtrlNbr;
        DateTime scheduledOnDutyUtc;
    DateTime defaultOffDutyUtc;
        DateTime requestedOnDutyUtc;
        int lateCallThresholdMinutes;

        await using (var uow = await uowFactory.CreateAsync(cancellationToken: ct))
        {
            var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(request.ShiftInstanceCtrlNbr, ct)
                ?? throw new KeyNotFoundException($"Shift instance {request.ShiftInstanceCtrlNbr.Value} not found.");

            var slot = shift.PositionSlots.SingleOrDefault(s => s.CtrlNbr == request.PositionSlotCtrlNbr)
                ?? throw new KeyNotFoundException($"Position slot {request.PositionSlotCtrlNbr.Value} not found on shift.");

            var slotIsClosed = slot.Status is PositionSlotStatus.Annulled
                or PositionSlotStatus.DoNotFill
                or PositionSlotStatus.TiedUp;

            if (slotIsClosed)
                throw new InvalidOperationException("Cannot fill a closed position slot.");

            resolvedCraftCtrlNbr = request.CraftCtrlNbr ?? await ResolveCraftCtrlNbrAsync(uow, slot, ct);
            var operationsPolicy = resolvedCraftCtrlNbr is null
                ? null
                : await uow.CraftOperationsPolicies.GetByCraftAsync(resolvedCraftCtrlNbr.Value, ct);

            if (shift.DepartmentCtrlNbr is not null)
            {
                var callSheetRule = await uow.CallSheetRules.GetByDepartmentAsync(shift.DepartmentCtrlNbr.Value);
                if (callSheetRule is { IsEnabled: false })
                    throw new InvalidOperationException("Vacancy fill is disabled by pool policy for this department.");
            }

            lateCallThresholdMinutes = operationsPolicy?.LateCallThresholdMinutes ?? 0;

            var work = await uow.WorkInstances.GetByCtrlNbrAsync(shift.WorkInstanceCtrlNbr, ct)
                ?? throw new KeyNotFoundException($"Work instance {shift.WorkInstanceCtrlNbr.Value} not found.");

            railroadCtrlNbr = (await uow.DynamicGroups.GetByCtrlNbrAsync(request.WorkAreaGroupCtrlNbr, ct))?.OwningRailroadCtrlNbr
                ?? throw new InvalidOperationException($"Unable to resolve railroad for work area {request.WorkAreaGroupCtrlNbr.Value}.");

            scheduledOnDutyUtc = DateTime.SpecifyKind(work.StartUtc.Date + slot.OnDutyTime.ToTimeSpan(), DateTimeKind.Utc);
            var defaultOffDutyDate = slot.OffDutyTime <= slot.OnDutyTime
                ? work.StartUtc.Date.AddDays(1)
                : work.StartUtc.Date;
            defaultOffDutyUtc = DateTime.SpecifyKind(defaultOffDutyDate + slot.OffDutyTime.ToTimeSpan(), DateTimeKind.Utc);
            requestedOnDutyUtc = request.ExpectedArrivalAtUtc
                ?? request.AcceptedAtUtc
                ?? DateTime.UtcNow;
        }

        var payload = new WorkflowPlaceOnDutyRuntimePayload(
            PositionSlotCtrlNbr: request.PositionSlotCtrlNbr,
            EmployeeCtrlNbr: request.EmployeeCtrlNbr,
            OnDutyTimeUtc: requestedOnDutyUtc,
            ScheduledOnDutyTimeUtc: scheduledOnDutyUtc,
            IsAssigned: true,
            LateCallThresholdMinutes: lateCallThresholdMinutes);

        var workflowExecuted = await workflowRuntimeService.ExecuteVacancyPlaceOnDutyRequestedAsync(
            railroadCtrlNbr,
            payload,
            correlationId: null,
            ct);

        if (!workflowExecuted)
        {
            await onDutyPlacementService.ExecuteAsync(
                request.PositionSlotCtrlNbr,
                request.EmployeeCtrlNbr,
                requestedOnDutyUtc,
                scheduledOnDutyUtc,
                isAssigned: true,
                lateCallThresholdMinutes,
                ct);
        }

        await using (var uow = await uowFactory.CreateAsync(cancellationToken: ct))
        {
            var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(request.ShiftInstanceCtrlNbr, ct)
                ?? throw new KeyNotFoundException($"Shift instance {request.ShiftInstanceCtrlNbr.Value} not found.");

            var slot = shift.PositionSlots.SingleOrDefault(s => s.CtrlNbr == request.PositionSlotCtrlNbr)
                ?? throw new KeyNotFoundException($"Position slot {request.PositionSlotCtrlNbr.Value} not found on shift.");

            var slotIsClosed = slot.Status is PositionSlotStatus.Annulled
                or PositionSlotStatus.DoNotFill
                or PositionSlotStatus.TiedUp;

            if (slotIsClosed)
                throw new InvalidOperationException("Cannot fill a closed position slot.");

            var existingFillForEmployee = slot.IncumbentEmployeeCtrlNbr == request.EmployeeCtrlNbr;
            var operationsPolicy = resolvedCraftCtrlNbr is null
                ? null
                : await uow.CraftOperationsPolicies.GetByCraftAsync(resolvedCraftCtrlNbr.Value, ct);
            var activeBoards = await uow.RosterBoards.GetActiveByWorkAreaAsync(request.WorkAreaGroupCtrlNbr, ct);

            await EnsureShiftBoardSlotCoverageAsync(
                uow,
                shift,
                activeBoards,
                resolvedCraftCtrlNbr,
                ct);

            var onDutyRecord = await ResolveOnDutyRecordAsync(uow, slot.CtrlNbr, request.EmployeeCtrlNbr, ct)
                ?? throw new InvalidOperationException("On-duty record was not created for filled vacancy.");

            var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(request.WorkAreaGroupCtrlNbr, ct);
            var railroad = workArea?.OwningRailroadCtrlNbr is null
                ? null
                : await uow.DynamicGroups.GetByCtrlNbrAsync(workArea.OwningRailroadCtrlNbr, ct);
            var workPeriodMode = railroad?.WorkPeriodMode ?? WorkPeriodMode.HalfMonth;
            var (workPeriodStartUtc, workPeriodEndUtc) = ResolveCurrentWorkPeriodBounds(workPeriodMode, onDutyRecord.OnDutyTimeUtc);
            var onDutyHistory = await uow.OnDutyRecords.GetOperationalForEmployeeInRangeAsync(
                request.EmployeeCtrlNbr,
                workPeriodStartUtc,
                workPeriodEndUtc,
                ct);
            var daysWorked = onDutyHistory
                .Select(r => DateTime.SpecifyKind(r.OnDutyTimeUtc, DateTimeKind.Utc).Date)
                .Distinct()
                .Count();

            if (!existingFillForEmployee)
                slot.Fill(request.EmployeeCtrlNbr, isIncumbent: true);

            slot.MarkOnDuty();

            await ApplyBoardSideEffectsAsync(
                uow,
                shift,
                request.EmployeeCtrlNbr,
                request.ForceOverride,
                operationsPolicy,
                resolvedCraftCtrlNbr,
                onDutyRecord,
                daysWorked,
                defaultOffDutyUtc,
                activeBoards,
                ct);

            await vacancyProjectionSyncService.ReconcileFromShiftChangeAsync(uow, shift, request.PositionSlotCtrlNbr, ct);

            var finalStatus = request.ForceOverride ? VacancyFillStatusCodes.FilledForced : VacancyFillStatusCodes.Filled;
            var decisionJson = $"{{\"ForceOverride\":{request.ForceOverride.ToString().ToLowerInvariant()},\"Accepted\":{request.Accepted.ToString().ToLowerInvariant()}}}";
            uow.DispatchDecisionLogs.Add(DispatchDecisionLog.Create(
                slot.CtrlNbr,
                DateTime.UtcNow,
                phase: "Fill",
                selectedEmployeeCtrlNbr: request.EmployeeCtrlNbr,
                selectionSource: "VacancyResolution",
                decisionJson: decisionJson));

            var fillLog = VacancyFillLog.Create(
                request.WorkAreaGroupCtrlNbr,
                request.ShiftInstanceCtrlNbr,
                request.PositionSlotCtrlNbr,
                request.EmployeeCtrlNbr,
                onDutyRecord.CtrlNbr,
                assignmentCode: slot.AssignmentCode,
                craftRoleName: slot.CraftRoleName,
                forceOverride: request.ForceOverride,
                forceReason: normalizedForceReason,
                accepted: request.Accepted,
                acceptedAtUtc: request.AcceptedAtUtc,
                isLateCall: request.IsLateCall,
                lateCallNote: normalizedLateCallNote,
                arrivalFollowUpNote: normalizedArrivalFollowUpNote,
                dispatcherNote: normalizedDispatcherNote,
                status: finalStatus);

            uow.VacancyFillLogs.Add(fillLog);
            await uow.ShiftInstances.UpdateAsync(shift, ct);
            await uow.CommitAsync(ct);

            return new VacancyFillResult(
                Success: true,
                Status: finalStatus,
                ShiftInstanceCtrlNbr: request.ShiftInstanceCtrlNbr,
                PositionSlotCtrlNbr: request.PositionSlotCtrlNbr,
                EmployeeCtrlNbr: request.EmployeeCtrlNbr,
                OnDutyRecordCtrlNbr: onDutyRecord.CtrlNbr,
                VacancyFillLogCtrlNbr: fillLog.CtrlNbr);
        }
    }

    public async Task<IReadOnlyList<VacancyFillAuditRecordDto>> GetAuditReportAsync(
        ControlNumber workAreaGroupCtrlNbr,
        DateOnly targetDate,
        ControlNumber? departmentCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var startUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var logs = await uow.VacancyFillLogs.GetByWorkAreaAndDateRangeAsync(
            workAreaGroupCtrlNbr,
            startUtc,
            endUtc,
            departmentCtrlNbr,
            ct);

        var employeeInfo = await uow.Employees.GetByCtrlNbrsAsync(logs.Select(l => l.EmployeeCtrlNbr).Distinct(), ct);
        var employeeNameByCtrlNbr = employeeInfo.ToDictionary(e => e.CtrlNbr, e => e.EmployeeNumber);

        return logs.Select(l => new VacancyFillAuditRecordDto(
            l.CtrlNbr,
            l.ShiftInstanceCtrlNbr,
            l.PositionSlotCtrlNbr,
            l.AssignmentCode,
            l.CraftRoleName,
            l.EmployeeCtrlNbr,
            employeeNameByCtrlNbr.GetValueOrDefault(l.EmployeeCtrlNbr, string.Empty),
            l.Status,
            l.ForceOverride,
            l.ForceReason,
            l.IsLateCall,
            l.LateCallNote,
            l.ArrivalFollowUpNote,
            l.DispatcherNote,
            l.CreatedAtUtc,
            l.WorkAreaGroupCtrlNbr)).ToList();
    }

    private static IReadOnlyList<VacancyCandidateContactDto> BuildContacts(Domain.Models.Employees.Employee? employee)
    {
        if (employee is null)
            return [];

        var contacts = new List<VacancyCandidateContactDto>();
        contacts.AddRange(employee.PhoneNumbers
            .OrderBy(p => p.CallingOrder)
            .Select(p => new VacancyCandidateContactDto("Phone", p.Number, p.CallingOrder)));

        contacts.AddRange(employee.EmailAddresses
            .Select((e, index) => new VacancyCandidateContactDto("Email", e.Email, index + 1)));

        return contacts;
    }

    private async Task<IReadOnlyList<BoardSlotInstance>> GetCandidateBoardRowsAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber workAreaGroupCtrlNbr,
        ShiftInstance shift,
        ControlNumber? craftCtrlNbr,
        CancellationToken ct)
    {
        var activeBoards = await uow.RosterBoards.GetActiveByWorkAreaAsync(workAreaGroupCtrlNbr, ct);
        var scopedBoards = activeBoards
            .Where(b => craftCtrlNbr is null || b.CraftCtrlNbr == craftCtrlNbr)
            .ToList();

        HashSet<BoardType>? candidateBoardTypes = null;
        if (craftCtrlNbr is not null)
        {
            var policy = await uow.BoardCascadePolicies.GetByWorkAreaAndCraftAsync(workAreaGroupCtrlNbr, craftCtrlNbr.Value);
            if (!string.IsNullOrWhiteSpace(policy?.SelectionStrategy))
                candidateBoardTypes = ParseBoardTypeStrategy(policy.SelectionStrategy);
        }

        var candidateBoardIds = scopedBoards
            .Where(b => candidateBoardTypes is null || candidateBoardTypes.Contains(b.BoardType))
            .Select(b => b.CtrlNbr)
            .ToHashSet();

        return shift.BoardSlots
            .Where(b => candidateBoardIds.Contains(b.RosterBoardCtrlNbr))
            .Where(b => b.Status == BoardSlotStatus.Available)
            .OrderBy(b => b.BoardOrder)
            .ThenBy(b => b.CallSequence)
            .ThenBy(b => b.CtrlNbr.Value)
            .ToList();
    }

    private static HashSet<BoardType> ParseBoardTypeStrategy(string? selectionStrategy)
    {
        if (string.IsNullOrWhiteSpace(selectionStrategy))
            throw new InvalidOperationException("Board selection strategy is required and cannot be empty.");

        var tokens = selectionStrategy
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.Trim())
            .ToArray();

        var types = new HashSet<BoardType>();
        foreach (var token in tokens)
        {
            if (Enum.TryParse<BoardType>(token, ignoreCase: true, out var parsed))
                types.Add(parsed);
        }

        if (types.Count == 0)
            throw new InvalidOperationException($"Board selection strategy '{selectionStrategy}' did not resolve to any valid board types.");

        return types;
    }

    private async Task<ControlNumber?> ResolveCraftCtrlNbrAsync(
        IOrchestrationUnitOfWork uow,
        PositionSlotInstance slot,
        CancellationToken ct)
    {
        if (slot.CrewPositionCtrlNbr is null)
            return null;

        var crewPosition = await uow.CrewPositions.GetByCtrlNbrAsync(slot.CrewPositionCtrlNbr.Value, ct);
        if (crewPosition is null)
            return null;

        var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(crewPosition.CraftRoleCtrlNbr, ct);
        return craftRole?.CraftCtrlNbr;
    }

    private static async Task<OnDutyRecord?> ResolveOnDutyRecordAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber positionSlotCtrlNbr,
        ControlNumber employeeCtrlNbr,
        CancellationToken ct)
    {
        var records = await uow.OnDutyRecords.GetByPositionSlotsAsync([positionSlotCtrlNbr], ct);
        return records
            .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(r => r.OnDutyTimeUtc)
            .FirstOrDefault();
    }

    private static async Task<ControlNumber?> GetProjectedEmployeeCtrlNbrAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber positionSlotCtrlNbr)
    {
        var projections = await uow.DispatchProjections.GetByPositionSlotAsync(positionSlotCtrlNbr);
        return projections.FirstOrDefault()?.ProjectedEmployeeCtrlNbr;
    }

    private static async Task ApplyBoardSideEffectsAsync(
        IOrchestrationUnitOfWork uow,
        ShiftInstance shift,
        ControlNumber employeeCtrlNbr,
        bool forceOverride,
        CraftOperationsPolicy? operationsPolicy,
        ControlNumber? craftCtrlNbr,
        OnDutyRecord onDutyRecord,
        int daysWorked,
        DateTime defaultOffDutyUtc,
        IReadOnlyList<RosterBoard> activeBoards,
        CancellationToken ct)
    {
        var candidateBoards = activeBoards
            .Where(b => craftCtrlNbr is null || b.CraftCtrlNbr == craftCtrlNbr)
            .Where(b => b.Positions.Any(p => p.EmployeeCtrlNbr == employeeCtrlNbr))
            .ToList();

        if (candidateBoards.Count != 1)
            throw new InvalidOperationException(
                $"Unable to resolve authoritative roster board for employee {employeeCtrlNbr.Value}. Matched boards: {candidateBoards.Count}.");

        var selectedRosterBoard = candidateBoards[0];

        EnsureAuthoritativeBoardSlotExists(shift, selectedRosterBoard, employeeCtrlNbr);

        var selectedBoardSlot = shift.BoardSlots
            .Where(b => b.EmployeeCtrlNbr == employeeCtrlNbr)
            .Where(b => b.RosterBoardCtrlNbr == selectedRosterBoard.CtrlNbr)
            .OrderBy(b => b.BoardOrder)
            .ThenBy(b => b.CallSequence)
            .FirstOrDefault();

        var craftScopedBoardCtrlNbrs = activeBoards
            .Where(b => craftCtrlNbr is null || b.CraftCtrlNbr == craftCtrlNbr)
            .Select(b => b.CtrlNbr)
            .ToHashSet();

        var craftScopedBoardSlots = shift.BoardSlots
            .Where(b => craftScopedBoardCtrlNbrs.Contains(b.RosterBoardCtrlNbr))
            .ToList();

        var nextCallSequence = craftScopedBoardSlots.Count == 0
            ? 1L
            : craftScopedBoardSlots.Max(b => b.CallSequence) + 1L;

        var selectedPosition = selectedRosterBoard.Positions.FirstOrDefault(p => p.EmployeeCtrlNbr == employeeCtrlNbr)
            ?? throw new InvalidOperationException(
                $"Employee {employeeCtrlNbr.Value} is not on roster board {selectedRosterBoard.CtrlNbr.Value}.");

        if (selectedBoardSlot is null)
            throw new InvalidOperationException(
                $"Authoritative board slot is missing for employee {employeeCtrlNbr.Value} on shift {shift.CtrlNbr.Value}, board {selectedRosterBoard.CtrlNbr.Value}.");

        if (selectedBoardSlot is not null)
        {
            selectedBoardSlot.RecordCallSequence(nextCallSequence);
            selectedBoardSlot.UpdateOperationalTracking(daysWorked, onDutyRecord.ConsecutiveDays, selectedBoardSlot.RestAvailableAtUtc);
            selectedBoardSlot.Call();
            if (forceOverride)
                selectedBoardSlot.Reposition(1);
        }

        StampBoardOrderKeysFromCall(selectedRosterBoard, employeeCtrlNbr, selectedPosition.PositionOrder, defaultOffDutyUtc);
        ReorderBoardByProtectedKeys(selectedRosterBoard);
        await uow.RosterBoards.UpdateAsync(selectedRosterBoard, ct);

        if (operationsPolicy?.DeleteConflictingNextShift == true)
        {
            foreach (var boardSlot in shift.BoardSlots.Where(b => b.EmployeeCtrlNbr == employeeCtrlNbr && b.CtrlNbr != selectedBoardSlot?.CtrlNbr))
                boardSlot.MarkUnavailable();
        }

        await uow.ShiftInstances.UpdateAsync(shift, ct);
    }

    private static void StampBoardOrderKeysFromCall(
        RosterBoard board,
        ControlNumber employeeCtrlNbr,
        int calledBoardOrder,
        DateTime defaultOffDutyUtc)
    {
        var selectedPosition = board.Positions.FirstOrDefault(p => p.EmployeeCtrlNbr == employeeCtrlNbr);
        if (selectedPosition is null)
            throw new InvalidOperationException(
                $"Employee {employeeCtrlNbr.Value} is not on roster board {board.CtrlNbr.Value}.");

        if (calledBoardOrder <= 0)
            throw new InvalidOperationException(
                $"Called board order is invalid for employee {employeeCtrlNbr.Value} on roster board {board.CtrlNbr.Value}.");

        selectedPosition.SetOrderSeedBoardPosition(calledBoardOrder);
        selectedPosition.SetTieUpOrderUtc(defaultOffDutyUtc);
    }

    private static void ReorderBoardByProtectedKeys(RosterBoard board)
    {
        var orderedPositions = board.Positions
            .OrderBy(p => p.TieUpOrderUtc ?? DateTime.MinValue)
            .ThenBy(p => p.OrderSeedBoardPosition)
            .ThenBy(p => p.PositionOrder)
            .ThenBy(p => p.CtrlNbr.Value)
            .ToList();

        var ordering = new List<(ControlNumber PositionCtrlNbr, int NewOrder)>();
        var order = 1;
        foreach (var position in orderedPositions)
            ordering.Add((position.CtrlNbr, order++));

        board.ReorderPositions(ordering);
    }

    private static async Task EnsureShiftBoardSlotCoverageAsync(
        IOrchestrationUnitOfWork uow,
        ShiftInstance shift,
        IReadOnlyList<RosterBoard> activeBoards,
        ControlNumber? craftCtrlNbr,
        CancellationToken ct)
    {
        var scopedBoards = activeBoards
            .Where(b => craftCtrlNbr is null || b.CraftCtrlNbr == craftCtrlNbr)
            .Where(b => b.IsActive)
            .ToList();

        if (scopedBoards.Count == 0)
            return;

        var employeeCtrlNbrs = scopedBoards
            .SelectMany(b => b.Positions)
            .Select(p => p.EmployeeCtrlNbr)
            .Distinct()
            .ToList();

        var employees = await uow.Employees.GetByCtrlNbrsAsync(employeeCtrlNbrs, ct);
        var employeeNumberByCtrlNbr = employees.ToDictionary(e => e.CtrlNbr, e => e.EmployeeNumber);

        foreach (var board in scopedBoards)
        {
            foreach (var position in board.Positions
                         .OrderBy(p => p.PositionOrder)
                         .ThenBy(p => p.CtrlNbr.Value))
            {
                var exists = shift.BoardSlots.Any(b =>
                    b.RosterBoardCtrlNbr == board.CtrlNbr
                    && b.RosterBoardPositionCtrlNbr == position.CtrlNbr
                    && b.EmployeeCtrlNbr == position.EmployeeCtrlNbr);

                if (exists)
                    continue;

                var employeeNumber = employeeNumberByCtrlNbr.TryGetValue(position.EmployeeCtrlNbr, out var resolvedEmployeeNumber)
                    ? resolvedEmployeeNumber
                    : $"Emp #{position.EmployeeCtrlNbr.Value}";

                shift.AddBoardSlot(
                    board.CtrlNbr,
                    position.CtrlNbr,
                    position.EmployeeCtrlNbr,
                    position.PositionOrder,
                    0,
                    board.Name,
                    employeeNumber,
                    positionName: string.Empty,
                    daysWorked: 0,
                    consecutiveDays: 0,
                    restAvailableAtUtc: null);
            }
        }
    }

    private static void EnsureAuthoritativeBoardSlotExists(
        ShiftInstance shift,
        RosterBoard board,
        ControlNumber employeeCtrlNbr)
    {
        var exists = shift.BoardSlots
            .Any(b => b.EmployeeCtrlNbr == employeeCtrlNbr
                      && b.RosterBoardCtrlNbr == board.CtrlNbr);

        if (!exists)
            throw new InvalidOperationException(
                $"Authoritative board slot is missing for employee {employeeCtrlNbr.Value} on shift {shift.CtrlNbr.Value}, board {board.CtrlNbr.Value}.");
    }

    private static (DateTime StartUtc, DateTime EndUtc) ResolveCurrentWorkPeriodBounds(WorkPeriodMode mode, DateTime referenceUtc)
    {
        var day = DateTime.SpecifyKind(referenceUtc, DateTimeKind.Utc).Date;

        if (mode == WorkPeriodMode.Monthly)
        {
            var start = new DateTime(day.Year, day.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return (start, start.AddMonths(1));
        }

        if (mode == WorkPeriodMode.Weekly)
        {
            var start = day.AddDays(-(int)day.DayOfWeek);
            return (start, start.AddDays(7));
        }

        if (mode == WorkPeriodMode.BiWeekly)
        {
            var yearStart = new DateTime(day.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodIndex = (int)((day - yearStart).TotalDays / 14);
            var start = yearStart.AddDays(periodIndex * 14);
            return (start, start.AddDays(14));
        }

        if (day.Day <= 15)
        {
            var start = new DateTime(day.Year, day.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return (start, new DateTime(day.Year, day.Month, 16, 0, 0, 0, DateTimeKind.Utc));
        }

        var secondHalfStart = new DateTime(day.Year, day.Month, 16, 0, 0, 0, DateTimeKind.Utc);
        return (secondHalfStart, secondHalfStart.AddDays(-15).AddMonths(1));
    }
}

public sealed record VacancyCandidateContactDto(
    string ContactType,
    string ContactValue,
    int CallingOrder);

public sealed record VacancyFillCandidateDto(
    ControlNumber EmployeeCtrlNbr,
    string EmployeeNumber,
    string EmployeeName,
    string BoardType,
    int BoardOrder,
    long CallSequence,
    string QualificationStatus,
    string StatusDisplay,
    string ProjectedVacancyDisplay,
    string OnDutyDisplay,
    IReadOnlyList<VacancyCandidateContactDto> Contacts,
    ControlNumber BoardSlotInstanceCtrlNbr);

public sealed record VacancyFillRequest(
    ControlNumber WorkAreaGroupCtrlNbr,
    ControlNumber ShiftInstanceCtrlNbr,
    ControlNumber PositionSlotCtrlNbr,
    ControlNumber EmployeeCtrlNbr,
    bool ForceOverride,
    string? ForceReason,
    string? DispatcherNote,
    bool Accepted,
    bool IsLateCall,
    string? LateCallNote,
    string? ArrivalFollowUpNote,
    DateTime? AcceptedAtUtc,
    DateTime? ExpectedArrivalAtUtc,
    ControlNumber? CraftCtrlNbr);

public sealed record VacancyFillResult(
    bool Success,
    string Status,
    ControlNumber ShiftInstanceCtrlNbr,
    ControlNumber PositionSlotCtrlNbr,
    ControlNumber EmployeeCtrlNbr,
    ControlNumber OnDutyRecordCtrlNbr,
    ControlNumber VacancyFillLogCtrlNbr);

public sealed record VacancyFillAuditRecordDto(
    ControlNumber VacancyFillLogCtrlNbr,
    ControlNumber ShiftInstanceCtrlNbr,
    ControlNumber PositionSlotCtrlNbr,
    string AssignmentCode,
    string CraftRoleName,
    ControlNumber EmployeeCtrlNbr,
    string EmployeeName,
    string Status,
    bool ForceOverride,
    string? ForceReason,
    bool IsLateCall,
    string? LateCallNote,
    string? ArrivalFollowUpNote,
    string? DispatcherNote,
    DateTime CreatedAtUtc,
    ControlNumber WorkAreaGroupCtrlNbr);

public static class VacancyFillStatusCodes
{
    public const string Filled = "Filled";
    public const string FilledForced = "FilledForced";
}
