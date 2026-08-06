using CrewService.Application.VacancyAssignment;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class VacancyAssignmentService(IServiceProvider serviceProvider)
    : VacancyAssignmentSrvc.VacancyAssignmentSrvcBase
{
    public override async Task<VacancyResolutionRunResponse> TriggerResolution(
        TriggerResolutionRequest request, ServerCallContext context)
    {
        var engine = serviceProvider.GetRequiredService<VacancyResolutionEngine>();
        var run = await engine.ExecuteAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr),
            ControlNumber.Create(request.ShiftInstanceCtrlNbr),
            ControlNumber.Create(request.CraftCtrlNbr),
            options: null,
            context.CancellationToken);

        return MapRun(run);
    }

    public override Task<GetResolutionRunsResponse> GetResolutionRuns(
        GetResolutionRunsRequest request, ServerCallContext context)
    {
        return Task.FromResult(new GetResolutionRunsResponse());
    }

    public override async Task<GetBoardSnapshotTimelineResponse> GetBoardSnapshotTimeline(
        GetBoardSnapshotTimelineRequest request,
        ServerCallContext context)
    {
        var uowFactory = serviceProvider.GetRequiredService<Domain.Interfaces.IOrchestrationUnitOfWorkFactory>();
        await using var uow = await uowFactory.CreateAsync(cancellationToken: context.CancellationToken);

        var shiftInstanceCtrlNbr = ControlNumber.Create(request.ShiftInstanceCtrlNbr);
        var snapshots = await uow.BoardSnapshots.GetByShiftInstanceAsync(shiftInstanceCtrlNbr, context.CancellationToken);

        var response = new GetBoardSnapshotTimelineResponse();
        response.Snapshots.AddRange(snapshots.Select(MapSnapshotTimelineItem));
        return response;
    }

    public override async Task<GetBoardSnapshotDetailResponse> GetBoardSnapshotDetail(
        GetBoardSnapshotDetailRequest request,
        ServerCallContext context)
    {
        var uowFactory = serviceProvider.GetRequiredService<Domain.Interfaces.IOrchestrationUnitOfWorkFactory>();
        await using var uow = await uowFactory.CreateAsync(cancellationToken: context.CancellationToken);

        var snapshotCtrlNbr = ControlNumber.Create(request.SnapshotCtrlNbr);
        var snapshot = await uow.BoardSnapshots.GetByCtrlNbrAsync(snapshotCtrlNbr, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Board snapshot {request.SnapshotCtrlNbr} not found."));

        return new GetBoardSnapshotDetailResponse
        {
            Snapshot = MapSnapshotDetail(snapshot)
        };
    }

    public override async Task<GetBoardSelectionDecisionsResponse> GetBoardSelectionDecisions(
        GetBoardSelectionDecisionsRequest request,
        ServerCallContext context)
    {
        var uowFactory = serviceProvider.GetRequiredService<Domain.Interfaces.IOrchestrationUnitOfWorkFactory>();
        await using var uow = await uowFactory.CreateAsync(cancellationToken: context.CancellationToken);

        var shiftInstanceCtrlNbr = ControlNumber.Create(request.ShiftInstanceCtrlNbr);
        var decisions = await uow.BoardSelectionDecisions.GetByShiftInstanceAsync(shiftInstanceCtrlNbr, context.CancellationToken);

        var response = new GetBoardSelectionDecisionsResponse();
        response.Decisions.AddRange(decisions.Select(MapDecisionItem));
        return response;
    }

    public override async Task<GetCurrentCallBoardResponse> GetCurrentCallBoard(
        GetCurrentCallBoardRequest request,
        ServerCallContext context)
    {
        var dailyOps = serviceProvider.GetRequiredService<Application.DailyOperations.DailyOperationsService>();
        var uowFactory = serviceProvider.GetRequiredService<Domain.Interfaces.IOrchestrationUnitOfWorkFactory>();
        var clock = serviceProvider.GetRequiredService<Application.Time.IWorkAreaClock>();
        var employeeNameService = serviceProvider.GetRequiredService<EmployeeNameService>();

        if (request.WorkAreaGroupCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "work_area_group_ctrl_nbr is required."));
        if (request.CraftCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "craft_ctrl_nbr is required."));
        if (string.IsNullOrWhiteSpace(request.BoardType))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "board_type is required."));
        if (!DateOnly.TryParse(request.TargetDateYyyyMmDd, out var targetDate))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "target_date_yyyy_mm_dd must be yyyy-MM-dd."));

        var workAreaCtrlNbr = ControlNumber.Create(request.WorkAreaGroupCtrlNbr);
        var craftCtrlNbr = ControlNumber.Create(request.CraftCtrlNbr);
        var shifts = await dailyOps.GetCallSheetAsync(workAreaCtrlNbr, targetDate, context.CancellationToken);

        await using var uow = await uowFactory.CreateAsync(cancellationToken: context.CancellationToken);
        var tz = await clock.GetWorkAreaTimeZoneAsync(uow, workAreaCtrlNbr, context.CancellationToken);

        var boards = await uow.RosterBoards.GetActiveByWorkAreaAsync(workAreaCtrlNbr, context.CancellationToken);
        var eligibleBoards = boards
            .Where(b => b.CraftCtrlNbr == craftCtrlNbr)
            .Where(b => string.Equals(b.BoardType.ToString(), request.BoardType, StringComparison.OrdinalIgnoreCase))
            .OrderBy(b => b.Name)
            .ThenBy(b => b.BoardType.ToString())
            .ToList();

        if (eligibleBoards.Count == 0)
            return new GetCurrentCallBoardResponse();

        var boardCtrlNbrs = eligibleBoards.Select(b => b.CtrlNbr).ToHashSet();
        var boardTypeByCtrlNbr = new Dictionary<ControlNumber, string>();
        foreach (var board in eligibleBoards)
        {
            boardTypeByCtrlNbr[board.CtrlNbr] = board.BoardType.ToString();
        }

        var employeeInfoByCtrlNbr = await employeeNameService.GetEmployeeInfoBatchAsync(
            uow,
            eligibleBoards
                .SelectMany(b => b.Positions)
                .Select(p => p.EmployeeCtrlNbr)
                .Distinct(),
            context.CancellationToken);

        var boardEmployeeCtrlNbrs = eligibleBoards
            .SelectMany(b => b.Positions)
            .Select(p => p.EmployeeCtrlNbr)
            .Distinct()
            .ToList();

        var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(workAreaCtrlNbr, context.CancellationToken);
        var railroadCtrlNbr = workArea?.OwningRailroadCtrlNbr;
        var railroad = railroadCtrlNbr is null
            ? null
            : await uow.DynamicGroups.GetByCtrlNbrAsync(railroadCtrlNbr, context.CancellationToken);
        var workPeriodMode = railroad?.WorkPeriodMode ?? WorkPeriodMode.HalfMonth;
        var (workPeriodStartUtc, workPeriodEndUtc) = ResolveCurrentWorkPeriodBounds(workPeriodMode, clock.UtcNow.UtcDateTime);

        var fraConsecutiveDaysByEmployee = new Dictionary<ControlNumber, int>();
        var workPeriodDaysWorkedByEmployee = new Dictionary<ControlNumber, int>();

        foreach (var employeeCtrlNbr in boardEmployeeCtrlNbrs)
        {
            var activeTour = await uow.FraDutyTours.GetActiveTourForEmployeeAsync(employeeCtrlNbr, context.CancellationToken);
            var consecutiveDays = activeTour?.ConsecutiveDays ?? 0;

            if (consecutiveDays == 0)
            {
                var tours = await uow.FraDutyTours.SearchAsync(
                    new Domain.Modules.FraCompliance.FraRecordSearchCriteria
                    {
                        EmployeeCtrlNbr = employeeCtrlNbr,
                        EndDateUtc = clock.UtcNow.UtcDateTime
                    },
                    context.CancellationToken);

                consecutiveDays = tours
                    .OrderByDescending(t => t.DutyTourStartUtc)
                    .Select(t => t.ConsecutiveDays)
                    .FirstOrDefault();
            }

            fraConsecutiveDaysByEmployee[employeeCtrlNbr] = consecutiveDays;

            var onDutyHistory = await uow.OnDutyRecords.GetForEmployeeInRangeAsync(
                employeeCtrlNbr,
                workPeriodStartUtc,
                workPeriodEndUtc,
                context.CancellationToken);

            var daysWorked = onDutyHistory
                .Select(r => DateTime.SpecifyKind(r.OnDutyTimeUtc, DateTimeKind.Utc).Date)
                .Distinct()
                .Count();

            workPeriodDaysWorkedByEmployee[employeeCtrlNbr] = daysWorked;
        }

        var operationalByBoardAndEmployee = new Dictionary<(long BoardCtrlNbr, long EmployeeCtrlNbr), Domain.Modules.WorkManagement.BoardSlotInstance>();
        var rest24ByEmployee = new Dictionary<ControlNumber, DateTime>();

        foreach (var shift in shifts)
        {
            foreach (var slot in shift.BoardSlots.Where(s => boardCtrlNbrs.Contains(s.RosterBoardCtrlNbr)))
            {
                var key = (slot.RosterBoardCtrlNbr.Value, slot.EmployeeCtrlNbr.Value);
                if (!operationalByBoardAndEmployee.TryGetValue(key, out var existing)
                    || slot.CallSequence > existing.CallSequence
                    || (slot.CallSequence == existing.CallSequence && slot.BoardOrder < existing.BoardOrder))
                {
                    operationalByBoardAndEmployee[key] = slot;
                }
            }

            var shiftSlotIds = shift.PositionSlots.Select(s => s.CtrlNbr).ToList();
            var shiftOnDutyRecords = await uow.OnDutyRecords.GetByPositionSlotsAsync(shiftSlotIds, context.CancellationToken);
            var latestOnDutyBySlot = shiftOnDutyRecords
                .OrderByDescending(r => r.OnDutyTimeUtc)
                .GroupBy(r => r.PositionSlotCtrlNbr)
                .ToDictionary(g => g.Key, g => g.First());
            var shiftOffDutyRecords = await uow.OffDutyRecords.GetByOnDutyRecordsAsync(
                latestOnDutyBySlot.Values.Select(r => r.CtrlNbr).ToList(),
                context.CancellationToken);
            var latestOffDutyByOnDuty = shiftOffDutyRecords
                .OrderByDescending(r => r.OffDutyTimeUtc)
                .GroupBy(r => r.OnDutyRecordCtrlNbr)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var positionSlot in shift.PositionSlots)
            {
                if (positionSlot.IncumbentEmployeeCtrlNbr is not { } employeeCtrlNbr)
                    continue;

                if (!latestOnDutyBySlot.TryGetValue(positionSlot.CtrlNbr, out var onDutyRecord))
                    continue;

                if (!latestOffDutyByOnDuty.TryGetValue(onDutyRecord.CtrlNbr, out var offDutyRecord))
                    continue;

                var rest24Utc = offDutyRecord.TwentyFourHourRestAtUtc;

                if (!rest24ByEmployee.TryGetValue(employeeCtrlNbr, out var existingRest)
                    || DateTime.SpecifyKind(rest24Utc, DateTimeKind.Utc) > existingRest)
                {
                    rest24ByEmployee[employeeCtrlNbr] = DateTime.SpecifyKind(rest24Utc, DateTimeKind.Utc);
                }
            }
        }

        var vacancyProjectionByEmployee = new Dictionary<ControlNumber, (Domain.Modules.WorkManagement.PositionSlotInstance Slot, ControlNumber ProjectedEmployeeCtrlNbr)>();
        var allPositionSlots = shifts
            .SelectMany(s => s.PositionSlots)
            .ToList();

        foreach (var slot in allPositionSlots)
        {
            var projections = await uow.DispatchProjections.GetByPositionSlotAsync(slot.CtrlNbr);
            var latestProjection = projections.FirstOrDefault();
            if (latestProjection?.ProjectedEmployeeCtrlNbr is not { } projectedEmployeeCtrlNbr)
                continue;

            if (!vacancyProjectionByEmployee.TryGetValue(projectedEmployeeCtrlNbr, out var existingProjection)
                || ComparePositionSlotOrdering(slot, existingProjection.Slot) < 0)
            {
                vacancyProjectionByEmployee[projectedEmployeeCtrlNbr] = (slot, projectedEmployeeCtrlNbr);
            }
        }

        var response = new GetCurrentCallBoardResponse();
        var rows = eligibleBoards
            .SelectMany(board => board.Positions
                .OrderBy(position => position.PositionOrder)
                .ThenBy(position => position.CtrlNbr.Value)
                .Select(position =>
                {
                    operationalByBoardAndEmployee.TryGetValue((board.CtrlNbr.Value, position.EmployeeCtrlNbr.Value), out var opRow);
                    vacancyProjectionByEmployee.TryGetValue(position.EmployeeCtrlNbr, out var projectedProjection);
                    var projectedSlot = projectedProjection.Slot;
                    rest24ByEmployee.TryGetValue(position.EmployeeCtrlNbr, out var rest24Utc);
                    return MapCurrentCallBoardRow(
                        board,
                        position,
                        opRow,
                        projectedSlot,
                        projectedProjection.ProjectedEmployeeCtrlNbr,
                        rest24Utc == default ? null : rest24Utc,
                        boardTypeByCtrlNbr,
                        employeeInfoByCtrlNbr,
                        fraConsecutiveDaysByEmployee,
                        workPeriodDaysWorkedByEmployee,
                        tz,
                        clock.UtcNow.UtcDateTime);
                }))
            .OrderBy(r => r.BoardName)
            .ThenBy(r => r.RowNumber)
            .ThenBy(r => r.EmployeeName)
            .ToList();

        for (var i = 0; i < rows.Count; i++)
            rows[i].RowNumber = i + 1;

        response.Rows.AddRange(rows);

        return response;
    }

    private static VacancyResolutionRunResponse MapRun(Domain.Modules.Dispatching.VacancyResolutionRun run)
    {
        var resp = new VacancyResolutionRunResponse
        {
            CtrlNbr = run.CtrlNbr.Value,
            Status = run.Status,
            SlotsEvaluated = run.SlotsEvaluated,
            SlotsFilled = run.SlotsFilled,
            StartedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(run.StartedAtUtc, DateTimeKind.Utc)),
        };
        if (run.CompletedAtUtc.HasValue)
            resp.CompletedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(run.CompletedAtUtc.Value, DateTimeKind.Utc));
        return resp;
    }

    private static BoardSnapshotTimelineItem MapSnapshotTimelineItem(Domain.Modules.WorkManagement.BoardSnapshot snapshot)
    {
        var item = new BoardSnapshotTimelineItem
        {
            SnapshotCtrlNbr = snapshot.CtrlNbr.Value,
            ShiftInstanceCtrlNbr = snapshot.ShiftInstanceCtrlNbr.Value,
            PositionSlotInstanceCtrlNbr = snapshot.PositionSlotInstanceCtrlNbr?.Value ?? 0,
            TriggerSource = snapshot.TriggerSource,
            DecisionSequence = snapshot.DecisionSequence,
            CapturedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(snapshot.CapturedAtUtc, DateTimeKind.Utc)),
            RowCount = snapshot.Rows.Count
        };

        if (snapshot.VacancyImpactCtrlNbr is not null)
            item.VacancyImpactCtrlNbr = snapshot.VacancyImpactCtrlNbr.Value;

        return item;
    }

    private static BoardSnapshotDetail MapSnapshotDetail(Domain.Modules.WorkManagement.BoardSnapshot snapshot)
    {
        var detail = new BoardSnapshotDetail
        {
            SnapshotCtrlNbr = snapshot.CtrlNbr.Value,
            ShiftInstanceCtrlNbr = snapshot.ShiftInstanceCtrlNbr.Value,
            PositionSlotInstanceCtrlNbr = snapshot.PositionSlotInstanceCtrlNbr?.Value ?? 0,
            TriggerSource = snapshot.TriggerSource,
            DecisionSequence = snapshot.DecisionSequence,
            CapturedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(snapshot.CapturedAtUtc, DateTimeKind.Utc))
        };

        if (snapshot.VacancyImpactCtrlNbr is not null)
            detail.VacancyImpactCtrlNbr = snapshot.VacancyImpactCtrlNbr.Value;

        detail.Rows.AddRange(snapshot.Rows
            .OrderBy(r => r.BoardOrder)
            .ThenBy(r => r.CallSequence)
            .ThenBy(r => r.CtrlNbr.Value)
            .Select(MapSnapshotRowDetail));

        return detail;
    }

    private static BoardSnapshotRowDetail MapSnapshotRowDetail(Domain.Modules.WorkManagement.BoardSnapshotRow row)
    {
        var detail = new BoardSnapshotRowDetail
        {
            BoardSnapshotRowCtrlNbr = row.CtrlNbr.Value,
            BoardSlotInstanceCtrlNbr = row.BoardSlotInstanceCtrlNbr.Value,
            ShiftInstanceCtrlNbr = row.ShiftInstanceCtrlNbr.Value,
            RosterBoardCtrlNbr = row.RosterBoardCtrlNbr.Value,
            EmployeeCtrlNbr = row.EmployeeCtrlNbr.Value,
            BoardOrder = row.BoardOrder,
            CallSequence = row.CallSequence,
            Status = row.Status,
            BoardName = row.BoardName,
            EmployeeName = row.EmployeeName,
            PositionName = row.PositionName
        };

        if (row.RosterBoardPositionCtrlNbr is not null)
            detail.RosterBoardPositionCtrlNbr = row.RosterBoardPositionCtrlNbr.Value;

        if (row.TieUpAtUtc.HasValue)
            detail.TieUpAt = Timestamp.FromDateTime(DateTime.SpecifyKind(row.TieUpAtUtc.Value, DateTimeKind.Utc));

        return detail;
    }

    private static BoardSelectionDecisionItem MapDecisionItem(Domain.Modules.WorkManagement.BoardSelectionDecision decision)
    {
        var item = new BoardSelectionDecisionItem
        {
            DecisionCtrlNbr = decision.CtrlNbr.Value,
            ShiftInstanceCtrlNbr = decision.ShiftInstanceCtrlNbr.Value,
            PositionSlotInstanceCtrlNbr = decision.PositionSlotInstanceCtrlNbr.Value,
            OccurredAt = Timestamp.FromDateTime(DateTime.SpecifyKind(decision.OccurredAtUtc, DateTimeKind.Utc)),
            DecisionSequence = decision.DecisionSequence,
            DecisionSource = decision.DecisionSource,
            DecisionPhase = decision.DecisionPhase,
            DecisionJson = decision.DecisionJson ?? string.Empty
        };

        if (decision.VacancyImpactCtrlNbr is not null)
            item.VacancyImpactCtrlNbr = decision.VacancyImpactCtrlNbr.Value;

        if (decision.SnapshotCtrlNbr is not null)
            item.SnapshotCtrlNbr = decision.SnapshotCtrlNbr.Value;

        if (decision.SelectedBoardSlotInstanceCtrlNbr is not null)
            item.SelectedBoardSlotInstanceCtrlNbr = decision.SelectedBoardSlotInstanceCtrlNbr.Value;

        if (decision.SelectedEmployeeCtrlNbr is not null)
            item.SelectedEmployeeCtrlNbr = decision.SelectedEmployeeCtrlNbr.Value;

        return item;
    }

    private static CurrentCallBoardRow MapCurrentCallBoardRow(
        Domain.Modules.Boards.RosterBoard board,
        Domain.Modules.Boards.RosterBoardPosition position,
        Domain.Modules.WorkManagement.BoardSlotInstance? slot,
        Domain.Modules.WorkManagement.PositionSlotInstance? projectedSlot,
        ControlNumber? projectedEmployeeCtrlNbr,
        DateTime? twentyFourHourRestAtUtc,
        IReadOnlyDictionary<ControlNumber, string> boardTypeByCtrlNbr,
        IReadOnlyDictionary<ControlNumber, (string FullNameLnf, string EmployeeNumber)> employeeInfoByCtrlNbr,
        IReadOnlyDictionary<ControlNumber, int> fraConsecutiveDaysByEmployee,
        IReadOnlyDictionary<ControlNumber, int> workPeriodDaysWorkedByEmployee,
        TimeZoneInfo? workAreaTimeZone,
        DateTime utcNow)
    {
        var resolvedStatus = ResolveLegacyStatus(slot, utcNow, workAreaTimeZone);
        var resolvedRestDisplay = ResolveRestTimeDisplay(twentyFourHourRestAtUtc, workAreaTimeZone);
        var resolvedConsecutiveDaysValue = slot is not null
            ? slot.ConsecutiveDays
            : fraConsecutiveDaysByEmployee.GetValueOrDefault(position.EmployeeCtrlNbr, 0);
        var resolvedDaysWorkedValue = slot is not null
            ? slot.DaysWorked
            : workPeriodDaysWorkedByEmployee.GetValueOrDefault(position.EmployeeCtrlNbr, 0);
        var resolvedConsecutiveDays = resolvedConsecutiveDaysValue.ToString();
        var resolvedDaysWorked = resolvedDaysWorkedValue.ToString();
        var resolvedTieUpOrderSeed = position.OrderSeedBoardPosition > 0
            ? position.OrderSeedBoardPosition
            : throw new InvalidOperationException(
                $"Missing OrderSeedBoardPosition for roster board position {position.CtrlNbr.Value} (employee {position.EmployeeCtrlNbr.Value}).");
        var resolvedBoardPosition = position.PositionOrder > 0
            ? position.PositionOrder.ToString()
            : "—";

        employeeInfoByCtrlNbr.TryGetValue(position.EmployeeCtrlNbr, out var employeeInfo);
        var authoritativeEmployeeName = !string.IsNullOrWhiteSpace(employeeInfo.FullNameLnf)
            ? employeeInfo.FullNameLnf
            : "Name Not Available";

        var row = new CurrentCallBoardRow
        {
            BoardSlotInstanceCtrlNbr = slot?.CtrlNbr.Value ?? 0,
            ShiftInstanceCtrlNbr = slot?.ShiftInstanceCtrlNbr.Value ?? 0,
            RosterBoardCtrlNbr = board.CtrlNbr.Value,
            EmployeeCtrlNbr = position.EmployeeCtrlNbr.Value,
            BoardOrder = resolvedTieUpOrderSeed > 0 ? resolvedTieUpOrderSeed : int.MaxValue,
            CallSequence = slot?.CallSequence ?? 0,
            Status = slot?.Status.ToString() ?? "Available",
            BoardName = board.Name,
            BoardType = boardTypeByCtrlNbr.TryGetValue(board.CtrlNbr, out var boardType)
                ? boardType
                : BoardType.ExtraBoard.ToString(),
            EmployeeName = authoritativeEmployeeName,
            PositionName = slot?.PositionName ?? string.Empty,
            DaysWorked = resolvedDaysWorkedValue,
            ConsecutiveDays = resolvedConsecutiveDaysValue,
            RowNumber = position.PositionOrder,
            StatusDisplay = resolvedStatus,
            RestTimeDisplay = resolvedRestDisplay,
            ConsecutiveDaysDisplay = resolvedConsecutiveDays,
            DaysWorkedDisplay = resolvedDaysWorked,
            BoardPositionDisplay = resolvedBoardPosition,
            ProjectedVacancyDisplay = ResolveProjectedVacancyDisplay(projectedSlot),
            ProjectedEmployeeDisplay = ResolveProjectedEmployeeDisplay(projectedEmployeeCtrlNbr, employeeInfoByCtrlNbr),
            OnDutyDisplay = ResolveProjectedOnDutyDisplay(projectedSlot)
        };

        if (position.CtrlNbr is not null)
            row.RosterBoardPositionCtrlNbr = position.CtrlNbr.Value;

        if (position.TieUpOrderUtc is not null)
            row.TieUpAt = Timestamp.FromDateTime(DateTime.SpecifyKind(position.TieUpOrderUtc.Value, DateTimeKind.Utc));

        if (twentyFourHourRestAtUtc is not null)
            row.RestAvailableAt = Timestamp.FromDateTime(DateTime.SpecifyKind(twentyFourHourRestAtUtc.Value, DateTimeKind.Utc));

        return row;
    }

    private static string ResolveEmployeeDisplayName(string? slotEmployeeName, string fallbackEmployeeName)
    {
        if (!string.IsNullOrWhiteSpace(slotEmployeeName)
            && !slotEmployeeName.StartsWith("Emp #", StringComparison.OrdinalIgnoreCase))
        {
            return slotEmployeeName;
        }

        return !string.IsNullOrWhiteSpace(fallbackEmployeeName)
            ? fallbackEmployeeName
            : "Name Not Available";
    }

    private static (DateTime StartUtc, DateTime EndUtc) ResolveCurrentWorkPeriodBounds(WorkPeriodMode mode, DateTime nowUtc)
    {
        var (startUtc, endUtc) = CurrentWorkPeriod(mode, nowUtc.Date);
        return (startUtc, endUtc);
    }

    private static (DateTime StartUtc, DateTime EndUtc) CurrentWorkPeriod(WorkPeriodMode mode, DateTime onDate)
    {
        var day = new DateTime(onDate.Year, onDate.Month, onDate.Day, 0, 0, 0, DateTimeKind.Utc);

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

    private static int ComparePositionSlotOrdering(
        Domain.Modules.WorkManagement.PositionSlotInstance x,
        Domain.Modules.WorkManagement.PositionSlotInstance y)
    {
        var xGroup = ResolveAssignmentOrderGroup(x.AssignmentCode);
        var yGroup = ResolveAssignmentOrderGroup(y.AssignmentCode);
        var cmp = xGroup.CompareTo(yGroup);
        if (cmp != 0)
            return cmp;

        var xNumeric = ResolveNumericAssignmentOrder(x.AssignmentCode);
        var yNumeric = ResolveNumericAssignmentOrder(y.AssignmentCode);
        cmp = xNumeric.CompareTo(yNumeric);
        if (cmp != 0)
            return cmp;

        cmp = string.Compare(x.AssignmentCode, y.AssignmentCode, StringComparison.OrdinalIgnoreCase);
        if (cmp != 0)
            return cmp;

        cmp = x.DisplayOrder.CompareTo(y.DisplayOrder);
        if (cmp != 0)
            return cmp;

        return x.CtrlNbr.Value.CompareTo(y.CtrlNbr.Value);
    }

    private static int ResolveAssignmentOrderGroup(string? assignmentCode)
        => long.TryParse(assignmentCode, out _) ? 0 : 1;

    private static long ResolveNumericAssignmentOrder(string? assignmentCode)
        => long.TryParse(assignmentCode, out var numeric) ? numeric : long.MaxValue;

    private static string ResolveProjectedVacancyDisplay(Domain.Modules.WorkManagement.PositionSlotInstance? projectedSlot)
    {
        if (projectedSlot is null)
            return "—";

        var roleName = string.IsNullOrWhiteSpace(projectedSlot.CraftRoleName)
            ? $"Position {projectedSlot.DisplayOrder}"
            : projectedSlot.CraftRoleName;

        if (string.IsNullOrWhiteSpace(projectedSlot.AssignmentCode))
            return roleName;

        return $"{projectedSlot.AssignmentCode} {roleName}";
    }

    private static string ResolveProjectedOnDutyDisplay(Domain.Modules.WorkManagement.PositionSlotInstance? projectedSlot)
        => projectedSlot is null
            ? "—"
            : projectedSlot.OnDutyTime.ToString("HH:mm");

    private static string ResolveProjectedEmployeeDisplay(
        ControlNumber? projectedEmployeeCtrlNbr,
        IReadOnlyDictionary<ControlNumber, (string FullNameLnf, string EmployeeNumber)> employeeInfoByCtrlNbr)
    {
        if (projectedEmployeeCtrlNbr is null)
            return "—";

        if (employeeInfoByCtrlNbr.TryGetValue(projectedEmployeeCtrlNbr, out var employeeInfo)
            && !string.IsNullOrWhiteSpace(employeeInfo.FullNameLnf))
        {
            return employeeInfo.FullNameLnf;
        }

        return projectedEmployeeCtrlNbr.Value.ToString();
    }

    private static string ResolveLegacyStatus(
        Domain.Modules.WorkManagement.BoardSlotInstance? row,
        DateTime utcNow,
        TimeZoneInfo? workAreaTimeZone)
    {
        if (row is null)
            return "Rested";

        return row.Status switch
        {
            Domain.Modules.WorkManagement.BoardSlotStatus.Called => string.IsNullOrWhiteSpace(row.PositionName) ? "Called" : $"Called for {row.PositionName}",
            Domain.Modules.WorkManagement.BoardSlotStatus.OnDuty => string.IsNullOrWhiteSpace(row.PositionName) ? "Working" : $"Working {row.PositionName}",
            Domain.Modules.WorkManagement.BoardSlotStatus.MarkedOff => "Marked Off",
            Domain.Modules.WorkManagement.BoardSlotStatus.Unavailable => "Not rested",
            _ => ResolveRestStatus(row, utcNow, workAreaTimeZone)
        };
    }

    private static string ResolveRestStatus(
        Domain.Modules.WorkManagement.BoardSlotInstance row,
        DateTime utcNow,
        TimeZoneInfo? workAreaTimeZone)
    {
        if (row.RestAvailableAtUtc is null)
            return "Rested";

        var restUtc = DateTime.SpecifyKind(row.RestAvailableAtUtc.Value, DateTimeKind.Utc);
        if (restUtc > utcNow)
        {
            var localRest = workAreaTimeZone is null
                ? restUtc
                : TimeZoneInfo.ConvertTimeFromUtc(restUtc, workAreaTimeZone);
            return $"Not rested until {localRest:MM/dd/yy hh:mm tt}";
        }

        return "Rested";
    }

    private static string ResolveRestTimeDisplay(
        DateTime? twentyFourHourRestAtUtc,
        TimeZoneInfo? workAreaTimeZone)
    {
        if (twentyFourHourRestAtUtc is null)
            return "—";

        var restUtc = DateTime.SpecifyKind(twentyFourHourRestAtUtc.Value, DateTimeKind.Utc);
        var localRest = workAreaTimeZone is null
            ? restUtc
            : TimeZoneInfo.ConvertTimeFromUtc(restUtc, workAreaTimeZone);
        return localRest.ToString("MM/dd/yy hh:mm tt");
    }
}
