using CrewService.Application.Workflows;
using CrewService.Application.Workflows.Effects;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public sealed class VacancyResolutionOrchestrationService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    WorkflowRuntimeService workflowRuntimeService,
    OnDutyPlacementService onDutyPlacementService)
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

        var employeesByCtrlNbr = new Dictionary<ControlNumber, Domain.Models.Employees.Employee>();
        foreach (var employeeCtrlNbr in boardRows.Select(b => b.EmployeeCtrlNbr).Distinct())
        {
            var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct);
            if (employee is not null)
                employeesByCtrlNbr[employeeCtrlNbr] = employee;
        }

        return boardRows
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
            .ToList();
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

        ControlNumber onDutyRecordCtrlNbr;
        string finalStatus;

        await using (var uow = await uowFactory.CreateAsync(cancellationToken: ct))
        {
            var shift = await uow.ShiftInstances.GetByCtrlNbrAsync(request.ShiftInstanceCtrlNbr, ct)
                ?? throw new KeyNotFoundException($"Shift instance {request.ShiftInstanceCtrlNbr.Value} not found.");

            var slot = shift.PositionSlots.SingleOrDefault(s => s.CtrlNbr == request.PositionSlotCtrlNbr)
                ?? throw new KeyNotFoundException($"Position slot {request.PositionSlotCtrlNbr.Value} not found on shift.");

            var existingFillForEmployee = slot.IncumbentEmployeeCtrlNbr == request.EmployeeCtrlNbr;
            var slotIsClosed = slot.Status is PositionSlotStatus.Annulled
                or PositionSlotStatus.DoNotFill
                or PositionSlotStatus.TiedUp;

            if (slotIsClosed)
                throw new InvalidOperationException("Cannot fill a closed position slot.");

            var resolvedCraftCtrlNbr = request.CraftCtrlNbr ?? await ResolveCraftCtrlNbrAsync(uow, slot, ct);
            var operationsPolicy = resolvedCraftCtrlNbr is null
                ? null
                : await uow.CraftOperationsPolicies.GetByCraftAsync(resolvedCraftCtrlNbr.Value, ct);

            var cascadePolicy = resolvedCraftCtrlNbr is null
                ? null
                : await uow.BoardCascadePolicies.GetByWorkAreaAndCraftAsync(request.WorkAreaGroupCtrlNbr, resolvedCraftCtrlNbr.Value);
            var activeBoards = await uow.RosterBoards.GetActiveByWorkAreaAsync(request.WorkAreaGroupCtrlNbr, ct);
            var boardTypeByCtrlNbr = activeBoards.ToDictionary(b => b.CtrlNbr, b => b.BoardType);

            if (shift.DepartmentCtrlNbr is not null)
            {
                var callSheetRule = await uow.CallSheetRules.GetByDepartmentAsync(shift.DepartmentCtrlNbr.Value);
                if (callSheetRule is { IsEnabled: false })
                    throw new InvalidOperationException("Vacancy fill is disabled by pool policy for this department.");
            }

            var lateCallThresholdMinutes = operationsPolicy?.LateCallThresholdMinutes ?? 0;

            var work = await uow.WorkInstances.GetByCtrlNbrAsync(shift.WorkInstanceCtrlNbr, ct)
                ?? throw new KeyNotFoundException($"Work instance {shift.WorkInstanceCtrlNbr.Value} not found.");

            var railroadCtrlNbr = (await uow.DynamicGroups.GetByCtrlNbrAsync(request.WorkAreaGroupCtrlNbr, ct))?.OwningRailroadCtrlNbr
                ?? throw new InvalidOperationException($"Unable to resolve railroad for work area {request.WorkAreaGroupCtrlNbr.Value}.");

            var scheduledOnDutyUtc = DateTime.SpecifyKind(work.StartUtc.Date + slot.OnDutyTime.ToTimeSpan(), DateTimeKind.Utc);
            var requestedOnDutyUtc = request.ExpectedArrivalAtUtc
                ?? request.AcceptedAtUtc
                ?? DateTime.UtcNow;

            var payload = new WorkflowPlaceOnDutyRuntimePayload(
                PositionSlotCtrlNbr: slot.CtrlNbr,
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
                    slot.CtrlNbr,
                    request.EmployeeCtrlNbr,
                    requestedOnDutyUtc,
                    scheduledOnDutyUtc,
                    isAssigned: true,
                    lateCallThresholdMinutes,
                    ct);
            }

            var onDutyRecord = await ResolveOnDutyRecordAsync(uow, slot.CtrlNbr, request.EmployeeCtrlNbr, ct)
                ?? throw new InvalidOperationException("On-duty record was not created for filled vacancy.");

            onDutyRecordCtrlNbr = onDutyRecord.CtrlNbr;

            if (!existingFillForEmployee)
                slot.Fill(request.EmployeeCtrlNbr, isIncumbent: true);

            slot.MarkOnDuty();

            await ApplyBoardSideEffectsAsync(
                uow,
                shift,
                request.EmployeeCtrlNbr,
                request.ForceOverride,
                operationsPolicy,
                cascadePolicy,
                boardTypeByCtrlNbr,
                ct);

            finalStatus = request.ForceOverride ? VacancyFillStatusCodes.FilledForced : VacancyFillStatusCodes.Filled;
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
                onDutyRecordCtrlNbr,
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
                OnDutyRecordCtrlNbr: onDutyRecordCtrlNbr,
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
            l.CreatedAtUtc)).ToList();
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
        var candidateBoardTypes = await ResolveCandidateBoardTypesAsync(uow, workAreaGroupCtrlNbr, craftCtrlNbr);
        var candidateBoardIds = activeBoards
            .Where(b => candidateBoardTypes.Contains(b.BoardType))
            .Where(b => craftCtrlNbr is null || b.CraftCtrlNbr == craftCtrlNbr)
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
            return [BoardType.ExtraBoard];

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

        return types.Count == 0 ? [BoardType.ExtraBoard] : types;
    }

    private async Task<HashSet<BoardType>> ResolveCandidateBoardTypesAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber? craftCtrlNbr)
    {
        if (craftCtrlNbr is null)
            return [BoardType.ExtraBoard];

        var policy = await uow.BoardCascadePolicies.GetByWorkAreaAndCraftAsync(workAreaGroupCtrlNbr, craftCtrlNbr.Value);
        return ParseBoardTypeStrategy(policy?.SelectionStrategy);
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

    private static async Task ApplyBoardSideEffectsAsync(
        IOrchestrationUnitOfWork uow,
        ShiftInstance shift,
        ControlNumber employeeCtrlNbr,
        bool forceOverride,
        CraftOperationsPolicy? operationsPolicy,
        BoardCascadePolicy? cascadePolicy,
        IReadOnlyDictionary<ControlNumber, BoardType> boardTypeByCtrlNbr,
        CancellationToken ct)
    {
        var allowedBoardTypes = ParseBoardTypeStrategy(cascadePolicy?.SelectionStrategy);
        var selectedBoardSlot = shift.BoardSlots
            .Where(b => b.EmployeeCtrlNbr == employeeCtrlNbr)
            .Where(b => boardTypeByCtrlNbr.TryGetValue(b.RosterBoardCtrlNbr, out var boardType) && allowedBoardTypes.Contains(boardType))
            .OrderBy(b => b.BoardOrder)
            .ThenBy(b => b.CallSequence)
            .FirstOrDefault();

        if (selectedBoardSlot is not null)
        {
            selectedBoardSlot.Call();
            selectedBoardSlot.MarkOnDuty();
            if (forceOverride)
                selectedBoardSlot.Reposition(1);
        }

        if (operationsPolicy?.DeleteConflictingNextShift == true)
        {
            foreach (var boardSlot in shift.BoardSlots.Where(b => b.EmployeeCtrlNbr == employeeCtrlNbr && b.CtrlNbr != selectedBoardSlot?.CtrlNbr))
                boardSlot.MarkUnavailable();
        }

        await uow.ShiftInstances.UpdateAsync(shift, ct);
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
    DateTime CreatedAtUtc);

public static class VacancyFillStatusCodes
{
    public const string Filled = "Filled";
    public const string FilledForced = "FilledForced";
}
