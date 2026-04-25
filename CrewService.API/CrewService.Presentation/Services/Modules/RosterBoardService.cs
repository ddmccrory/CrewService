using CrewService.Application.RosterBoardOps;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class RosterBoardService(
    EmployeeNameService employeeNameService,
    IServiceProvider serviceProvider)
    : RosterBoardSrvc.RosterBoardSrvcBase
{
    public override async Task<RosterBoardResponse> GetRosterBoard(
        GetRosterBoardRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RosterBoardAppService>();
        var (board, craftName, rosterName, workAreaCtrlNbr, workAreaName, labels) =
            await svc.GetRosterBoardDetailAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
        if (board is null) return new RosterBoardResponse();
        return await MapBoardAsync(board, craftName, rosterName, workAreaCtrlNbr, workAreaName, labels);
    }

    public override async Task<GetAllRosterBoardsResponse> GetAllRosterBoards(
        GetAllRosterBoardsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RosterBoardAppService>();
        var result = await svc.GetAllRosterBoardsAsync(
            request.CraftCtrlNbr, request.ParentCtrlNbr, request.DynamicGroupCtrlNbr, context.CancellationToken);

        var response = new GetAllRosterBoardsResponse();
        if (result.Boards.Count == 0)
        {
            response.TotalCount = 0;
            return response;
        }

        var allUserIds = result.EmployeeMap.Values
            .Select(e => e.UserId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList()!;
        var nameMap = await employeeNameService.GetFullNameLnfBatchAsync(allUserIds!);

        foreach (var board in result.Boards)
        {
            var craftName = board.CraftCtrlNbr is not null && result.CraftNames.TryGetValue(board.CraftCtrlNbr, out var cn) ? cn : string.Empty;
            var rosterName = string.Empty;
            long workAreaCtrlNbr = 0;
            var workAreaName = string.Empty;

            if (board.RosterCtrlNbr is not null && result.RosterMap.TryGetValue(board.RosterCtrlNbr, out var roster))
            {
                rosterName = roster.RosterName;
                workAreaCtrlNbr = roster.WorkAreaGroupCtrlNbr.Value;
                result.GroupNames.TryGetValue(roster.WorkAreaGroupCtrlNbr, out workAreaName);
            }

            var boardResponse = new RosterBoardResponse
            {
                CtrlNbr = board.CtrlNbr?.Value ?? 0,
                Name = board.Name,
                IsActive = board.IsActive,
                WorkAreaGroupCtrlNbr = workAreaCtrlNbr,
                WorkAreaName = workAreaName ?? string.Empty,
                CraftCtrlNbr = board.CraftCtrlNbr?.Value ?? 0,
                RosterCtrlNbr = board.RosterCtrlNbr?.Value ?? 0,
                BoardType = board.BoardType.ToString(),
                RotationType = board.RotationType.ToString(),
                RosterName = rosterName,
                CraftName = craftName
            };

            foreach (var position in board.Positions)
            {
                result.EmployeeMap.TryGetValue(position.EmployeeCtrlNbr!, out var emp);
                var userId = emp?.UserId ?? string.Empty;
                var fullName = !string.IsNullOrEmpty(userId) && nameMap.TryGetValue(userId, out var n) ? n : string.Empty;

                var posResponse = new RosterBoardPositionResponse
                {
                    CtrlNbr = position.CtrlNbr?.Value ?? 0,
                    EmployeeCtrlNbr = position.EmployeeCtrlNbr?.Value ?? 0,
                    PositionOrder = position.PositionOrder,
                    HangoutStatus = position.HangoutStatus,
                    HangoutAt = position.HangoutAtUtc.HasValue
                        ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                            DateTime.SpecifyKind(position.HangoutAtUtc.Value, DateTimeKind.Utc))
                        : null,
                    EmployeeNumber = emp?.EmployeeNumber ?? string.Empty,
                    EmployeeUserId = userId,
                    EmployeeFullNameLnf = fullName
                };
                if (position.EmployeeCtrlNbr is not null &&
                    result.RestrictionLabels.TryGetValue(position.EmployeeCtrlNbr, out var posLabels))
                    posResponse.RestrictionLabels.AddRange(posLabels);
                boardResponse.Positions.Add(posResponse);
            }
            response.Boards.Add(boardResponse);
        }

        response.TotalCount = response.Boards.Count;
        return response;
    }

    public override async Task<RosterBoardResponse> CreateRosterBoard(
        CreateRosterBoardRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RosterBoardAppService>();
        var boardType = Enum.Parse<BoardType>(request.BoardType, ignoreCase: true);
        var rotationType = Enum.Parse<RotationType>(request.RotationType, ignoreCase: true);
        var (board, craftName, rosterName, workAreaCtrlNbr, workAreaName) =
            await svc.CreateRosterBoardAsync(request.CraftCtrlNbr, request.RosterCtrlNbr, request.Name,
                boardType, rotationType, request.IsActive, context.CancellationToken);
        return await MapBoardAsync(board, craftName, rosterName, workAreaCtrlNbr, workAreaName, []);
    }

    public override async Task<RosterBoardResponse> UpdateRosterBoard(
        UpdateRosterBoardRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RosterBoardAppService>();
        try
        {
            var boardType = Enum.Parse<BoardType>(request.BoardType, ignoreCase: true);
            var rotationType = Enum.Parse<RotationType>(request.RotationType, ignoreCase: true);
            var (board, craftName, rosterName, workAreaCtrlNbr, workAreaName) =
                await svc.UpdateRosterBoardAsync(ControlNumber.Create(request.CtrlNbr), request.Name,
                    boardType, rotationType, request.IsActive, context.CancellationToken);
            return await MapBoardAsync(board, craftName, rosterName, workAreaCtrlNbr, workAreaName, []);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<DeleteResponse> DeleteRosterBoard(
        DeleteRosterBoardRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RosterBoardAppService>();
        try
        {
            var ctrlNbr = await svc.DeleteRosterBoardAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { $"Roster board {ctrlNbr?.Value ?? 0} deleted." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<RosterBoardPositionResponse> AddRosterBoardPosition(
        AddRosterBoardPositionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RosterBoardAppService>();
        try
        {
            var (position, labels) = await svc.AddRosterBoardPositionAsync(
                ControlNumber.Create(request.RosterBoardCtrlNbr),
                ControlNumber.Create(request.EmployeeCtrlNbr),
                request.PositionOrder, context.CancellationToken);
            return await MapPositionAsync(position, labels);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
    }

    public override async Task<DeleteResponse> RemoveRosterBoardPosition(
        RemoveRosterBoardPositionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RosterBoardAppService>();
        try
        {
            var ctrlNbr = await svc.RemoveRosterBoardPositionAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { $"Position {ctrlNbr.Value} removed." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<RosterBoardPositionResponse> HangoutPosition(
        HangoutPositionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RosterBoardAppService>();
        try
        {
            var (position, labels) = await svc.HangoutPositionAsync(ControlNumber.Create(request.PositionCtrlNbr), context.CancellationToken);
            return await MapPositionAsync(position, labels);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<RosterBoardPositionResponse> RestorePosition(
        RestorePositionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RosterBoardAppService>();
        try
        {
            var (position, labels) = await svc.RestorePositionAsync(ControlNumber.Create(request.PositionCtrlNbr), context.CancellationToken);
            return await MapPositionAsync(position, labels);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<RosterBoardResponse> ReorderRosterBoardPositions(
        ReorderRosterBoardPositionsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RosterBoardAppService>();
        try
        {
            var ordering = request.Entries
                .Select(e => (ControlNumber.Create(e.PositionCtrlNbr), e.PositionOrder))
                .ToList();
            var (board, craftName, rosterName, workAreaCtrlNbr, workAreaName) =
                await svc.ReorderRosterBoardPositionsAsync(
                    ControlNumber.Create(request.RosterBoardCtrlNbr), ordering, context.CancellationToken);
            return await MapBoardAsync(board, craftName, rosterName, workAreaCtrlNbr, workAreaName, []);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GetEligibleEmployeesForRosterBoardResponse> GetEligibleEmployeesForRosterBoard(
        GetEligibleEmployeesForRosterBoardRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<RosterBoardAppService>();
        var qualified = await svc.GetEligibleEmployeesForRosterBoardAsync(
            ControlNumber.Create(request.CraftCtrlNbr),
            ControlNumber.Create(request.ClientCtrlNbr),
            context.CancellationToken);

        var nameMap = await employeeNameService.GetFullNameLnfBatchAsync(qualified.Select(e => e.UserId));
        var eligible = qualified.Select(e => new EligibleEmployeeItem
        {
            CtrlNbr = e.CtrlNbr.Value,
            EmployeeNumber = e.EmployeeNumber,
            FullNameLnf = nameMap.GetValueOrDefault(e.UserId ?? string.Empty, string.Empty)
        }).ToList();

        eligible.Sort((a, b) => string.Compare(a.FullNameLnf, b.FullNameLnf, StringComparison.OrdinalIgnoreCase));
        var response = new GetEligibleEmployeesForRosterBoardResponse();
        response.Employees.AddRange(eligible);
        return response;
    }

    // ── Mapping ──────────────────────────────────────────────────────────────

    private async Task<RosterBoardResponse> MapBoardAsync(
        RosterBoard board, string craftName, string rosterName,
        long workAreaCtrlNbr, string workAreaName,
        Dictionary<ControlNumber, List<string>> empRestrictionLabels)
    {
        var response = new RosterBoardResponse
        {
            CtrlNbr = board.CtrlNbr?.Value ?? 0,
            Name = board.Name,
            IsActive = board.IsActive,
            WorkAreaGroupCtrlNbr = workAreaCtrlNbr,
            WorkAreaName = workAreaName,
            CraftCtrlNbr = board.CraftCtrlNbr?.Value ?? 0,
            RosterCtrlNbr = board.RosterCtrlNbr?.Value ?? 0,
            BoardType = board.BoardType.ToString(),
            RotationType = board.RotationType.ToString(),
            RosterName = rosterName,
            CraftName = craftName
        };
        foreach (var position in board.Positions)
            response.Positions.Add(await MapPositionAsync(position, empRestrictionLabels));
        return response;
    }

    private async Task<RosterBoardPositionResponse> MapPositionAsync(
        RosterBoardPosition position, Dictionary<ControlNumber, List<string>> empRestrictionLabels)
    {
        var fullName = await employeeNameService.GetFullNameLnfAsync(position.EmployeeCtrlNbr?.Value.ToString());
        var pr = new RosterBoardPositionResponse
        {
            CtrlNbr = position.CtrlNbr?.Value ?? 0,
            EmployeeCtrlNbr = position.EmployeeCtrlNbr?.Value ?? 0,
            PositionOrder = position.PositionOrder,
            HangoutStatus = position.HangoutStatus,
            HangoutAt = position.HangoutAtUtc.HasValue
                ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                    DateTime.SpecifyKind(position.HangoutAtUtc.Value, DateTimeKind.Utc))
                : null,
            EmployeeFullNameLnf = fullName
        };
        if (position.EmployeeCtrlNbr is not null &&
            empRestrictionLabels.TryGetValue(position.EmployeeCtrlNbr, out var labels))
            pr.RestrictionLabels.AddRange(labels);
        return pr;
    }
}
