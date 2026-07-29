using CrewService.Application.VacancyAssignment;
using CrewService.Domain.Modules.Boards;
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
            eligibleBoards
                .SelectMany(b => b.Positions)
                .Select(p => p.EmployeeCtrlNbr)
                .Distinct());

        var operationalByBoardAndEmployee = new Dictionary<(long BoardCtrlNbr, long EmployeeCtrlNbr), Domain.Modules.WorkManagement.BoardSlotInstance>();

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
        }

        var response = new GetCurrentCallBoardResponse();
        var rows = eligibleBoards
            .SelectMany(board => board.Positions.Select(position =>
            {
                operationalByBoardAndEmployee.TryGetValue((board.CtrlNbr.Value, position.EmployeeCtrlNbr.Value), out var opRow);
                return MapCurrentCallBoardRow(board, position, opRow, boardTypeByCtrlNbr, employeeInfoByCtrlNbr, tz, clock.UtcNow.UtcDateTime);
            }))
            .OrderBy(r => ResolveSortTieUpOrder(r))
            .ThenBy(r => ResolveSortBoardOrder(r))
            .ThenBy(r => r.BoardName)
            .ThenBy(r => r.EmployeeName)
            .ToList();

        if (string.Equals(request.BoardType, BoardType.ExtraBoard.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            rows = rows
                .Where(r => !string.Equals(r.Status, "HungOut", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

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
        IReadOnlyDictionary<ControlNumber, string> boardTypeByCtrlNbr,
        IReadOnlyDictionary<ControlNumber, (string FullNameLnf, string EmployeeNumber)> employeeInfoByCtrlNbr,
        TimeZoneInfo? workAreaTimeZone,
        DateTime utcNow)
    {
        var resolvedStatus = ResolveLegacyStatus(slot, utcNow, workAreaTimeZone);
        var resolvedRestDisplay = ResolveRestTimeDisplay(slot, workAreaTimeZone);
        var resolvedConsecutiveDays = slot?.ConsecutiveDays.ToString() ?? "—";
        var resolvedDaysWorked = slot?.DaysWorked.ToString() ?? "—";
        var resolvedBoardPosition = slot is null ? "—" : $"{slot.CallSequence}/{slot.BoardOrder}";

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
            BoardOrder = slot?.BoardOrder ?? int.MaxValue,
            CallSequence = slot?.CallSequence ?? 0,
            Status = slot?.Status.ToString() ?? "Available",
            BoardName = board.Name,
            BoardType = boardTypeByCtrlNbr.TryGetValue(board.CtrlNbr, out var boardType)
                ? boardType
                : BoardType.ExtraBoard.ToString(),
            EmployeeName = authoritativeEmployeeName,
            PositionName = slot?.PositionName ?? string.Empty,
            DaysWorked = slot?.DaysWorked ?? 0,
            ConsecutiveDays = slot?.ConsecutiveDays ?? 0,
            RowNumber = position.PositionOrder,
            StatusDisplay = resolvedStatus,
            RestTimeDisplay = resolvedRestDisplay,
            ConsecutiveDaysDisplay = resolvedConsecutiveDays,
            DaysWorkedDisplay = resolvedDaysWorked,
            BoardPositionDisplay = resolvedBoardPosition,
            ProjectedVacancyDisplay = "—",
            OnDutyDisplay = "—"
        };

        if (position.CtrlNbr is not null)
            row.RosterBoardPositionCtrlNbr = position.CtrlNbr.Value;

        if (slot?.TieUpAtUtc is not null)
            row.TieUpAt = Timestamp.FromDateTime(DateTime.SpecifyKind(slot.TieUpAtUtc.Value, DateTimeKind.Utc));

        if (slot?.RestAvailableAtUtc is not null)
            row.RestAvailableAt = Timestamp.FromDateTime(DateTime.SpecifyKind(slot.RestAvailableAtUtc.Value, DateTimeKind.Utc));

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

    private static long ResolveSortTieUpOrder(CurrentCallBoardRow row)
    {
        if (row.TieUpAt is null)
            return long.MaxValue;

        var utc = row.TieUpAt.ToDateTime().ToUniversalTime();
        return long.Parse(utc.ToString("yyyyMMddHHmm"));
    }

    private static int ResolveSortBoardOrder(CurrentCallBoardRow row)
        => row.BoardOrder;

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
        Domain.Modules.WorkManagement.BoardSlotInstance? row,
        TimeZoneInfo? workAreaTimeZone)
    {
        if (row?.RestAvailableAtUtc is null)
            return "—";

        var restUtc = DateTime.SpecifyKind(row.RestAvailableAtUtc.Value, DateTimeKind.Utc);
        var localRest = workAreaTimeZone is null
            ? restUtc
            : TimeZoneInfo.ConvertTimeFromUtc(restUtc, workAreaTimeZone);
        return localRest.ToString("MM/dd/yy hh:mm tt");
    }
}
