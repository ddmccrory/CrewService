using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.Infrastructure.Models.UserAccount;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;

namespace CrewService.Presentation.Services.Modules;

public class RosterBoardService(
    IRosterBoardRepository rosterBoardRepository,
    IRosterRepository rosterRepository,
    IEmployeeRepository employeeRepository,
    IDynamicGroupRepository dynamicGroupRepository,
    ICraftRepository craftRepository,
    IOrchestrationUnitOfWorkFactory uowFactory,
    UserManager<User> userManager)
    : RosterBoardSrvc.RosterBoardSrvcBase
{
    private readonly IRosterBoardRepository _rosterBoardRepository = rosterBoardRepository;
    private readonly IRosterRepository _rosterRepository = rosterRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IDynamicGroupRepository _dynamicGroupRepository = dynamicGroupRepository;
    private readonly ICraftRepository _craftRepository = craftRepository;
    private readonly UserManager<User> _userManager = userManager;

    public override async Task<RosterBoardResponse> GetRosterBoard(
        GetRosterBoardRequest request, ServerCallContext context)
    {
        var board = await _rosterBoardRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr));
        if (board is null) return new RosterBoardResponse();
        var craftName = await ResolveCraftNameAsync(board.CraftCtrlNbr);
        return await MapToResponseAsync(board, craftName);
    }

    public override async Task<GetAllRosterBoardsResponse> GetAllRosterBoards(
        GetAllRosterBoardsRequest request, ServerCallContext context)
    {
        var response = new GetAllRosterBoardsResponse();

        IReadOnlyList<RosterBoard> boards;

        if (request.CraftCtrlNbr > 0)
        {
            boards = await _rosterBoardRepository.GetByCraftCtrlNbrAsync(ControlNumber.Create(request.CraftCtrlNbr));
        }
        else if (request.ParentCtrlNbr > 0)
        {
            var crafts = await _craftRepository.GetByParentAndRailroadAsync(
                ControlNumber.Create(request.ParentCtrlNbr),
                request.DynamicGroupCtrlNbr > 0 ? ControlNumber.Create(request.DynamicGroupCtrlNbr) : null);

            var craftCtrlNbrs = crafts.Select(c => c.CtrlNbr).ToList();
            boards = craftCtrlNbrs.Count > 0
                ? await _rosterBoardRepository.GetByCraftCtrlNbrsAsync(craftCtrlNbrs)
                : [];
        }
        else
        {
            boards = await _rosterBoardRepository.GetAllAsync();
        }

        // Batch-resolve craft names
        var distinctCraftCtrlNbrs = boards.Select(b => b.CraftCtrlNbr).Distinct().ToList();
        var craftNames = new Dictionary<ControlNumber, string>();
        foreach (var ctrlNbr in distinctCraftCtrlNbrs)
        {
            var craft = await _craftRepository.GetByCtrlNbrAsync(ctrlNbr);
            if (craft is not null) craftNames[ctrlNbr] = craft.CraftName;
        }

        foreach (var board in boards)
        {
            craftNames.TryGetValue(board.CraftCtrlNbr, out var craftName);
            response.Boards.Add(await MapToResponseAsync(board, craftName ?? string.Empty));
        }

        response.TotalCount = response.Boards.Count;
        return response;
    }

    public override async Task<RosterBoardResponse> CreateRosterBoard(
        CreateRosterBoardRequest request, ServerCallContext context)
    {
        var boardType = Enum.Parse<BoardType>(request.BoardType, ignoreCase: true);
        var rotationType = Enum.Parse<RotationType>(request.RotationType, ignoreCase: true);

        var board = RosterBoard.Create(
            ControlNumber.Create(request.CraftCtrlNbr),
            ControlNumber.Create(request.RosterCtrlNbr),
            request.Name,
            boardType,
            rotationType,
            request.IsActive);

        await _rosterBoardRepository.AddAsync(board);
        var craftName = await ResolveCraftNameAsync(board.CraftCtrlNbr);
        return await MapToResponseAsync(board, craftName);
    }

    public override async Task<RosterBoardResponse> UpdateRosterBoard(
        UpdateRosterBoardRequest request, ServerCallContext context)
    {
        var board = await _rosterBoardRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Roster board {request.CtrlNbr} not found."));

        var boardType = Enum.Parse<BoardType>(request.BoardType, ignoreCase: true);
        var rotationType = Enum.Parse<RotationType>(request.RotationType, ignoreCase: true);

        board.Update(request.Name, boardType, rotationType, request.IsActive);
        await _rosterBoardRepository.UpdateAsync(board);
        var craftName = await ResolveCraftNameAsync(board.CraftCtrlNbr);
        return await MapToResponseAsync(board, craftName);
    }

    public override async Task<DeleteResponse> DeleteRosterBoard(
        DeleteRosterBoardRequest request, ServerCallContext context)
    {
        var board = await _rosterBoardRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Roster board {request.CtrlNbr} not found."));

        await _rosterBoardRepository.DeleteAsync(board.CtrlNbr);

        return new DeleteResponse
        {
            Success = true,
            Messages = { $"Roster board {board.CtrlNbr?.Value ?? 0} deleted." }
        };
    }

    public override async Task<RosterBoardPositionResponse> AddRosterBoardPosition(
        AddRosterBoardPositionRequest request, ServerCallContext context)
    {
        var board = await _rosterBoardRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.RosterBoardCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Roster board {request.RosterBoardCtrlNbr} not found."));

        var staffablePosition = StaffablePosition.Create("Board");
        var position = board.AddPosition(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            request.PositionOrder,
            staffablePosition.CtrlNbr);

        await using var uow = await uowFactory.CreateAsync();
        uow.StaffablePositions.Add(staffablePosition);
        uow.RosterBoards.Update(board);
        await uow.CommitAsync();

        return await MapPositionResponseAsync(position);
    }

    public override async Task<DeleteResponse> RemoveRosterBoardPosition(
        RemoveRosterBoardPositionRequest request, ServerCallContext context)
    {
        var boards = await _rosterBoardRepository.GetAllAsync();
        var board = boards.FirstOrDefault(b => b.Positions.Any(p => p.CtrlNbr == ControlNumber.Create(request.CtrlNbr)))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Position {request.CtrlNbr} not found on any board."));

        var position = board.Positions.First(p => p.CtrlNbr == ControlNumber.Create(request.CtrlNbr));
        board.RemovePosition(position);
        await _rosterBoardRepository.UpdateAsync(board);

        return new DeleteResponse
        {
            Success = true,
            Messages = { $"Position {request.CtrlNbr} removed." }
        };
    }

    public override async Task<RosterBoardPositionResponse> HangoutPosition(
        HangoutPositionRequest request, ServerCallContext context)
    {
        var boards = await _rosterBoardRepository.GetAllAsync();
        var board = boards.FirstOrDefault(b => b.Positions.Any(p => p.CtrlNbr == ControlNumber.Create(request.PositionCtrlNbr)))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Position {request.PositionCtrlNbr} not found."));

        var position = board.Positions.First(p => p.CtrlNbr == ControlNumber.Create(request.PositionCtrlNbr));
        position.Hangout();
        await _rosterBoardRepository.UpdateAsync(board);
        return await MapPositionResponseAsync(position);
    }

    public override async Task<RosterBoardPositionResponse> RestorePosition(
        RestorePositionRequest request, ServerCallContext context)
    {
        var boards = await _rosterBoardRepository.GetAllAsync();
        var board = boards.FirstOrDefault(b => b.Positions.Any(p => p.CtrlNbr == ControlNumber.Create(request.PositionCtrlNbr)))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Position {request.PositionCtrlNbr} not found."));

        var position = board.Positions.First(p => p.CtrlNbr == ControlNumber.Create(request.PositionCtrlNbr));
        position.RestoreFromHangout();
        await _rosterBoardRepository.UpdateAsync(board);
        return await MapPositionResponseAsync(position);
    }

    public override async Task<RosterBoardResponse> ReorderRosterBoardPositions(
        ReorderRosterBoardPositionsRequest request, ServerCallContext context)
    {
        var board = await _rosterBoardRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.RosterBoardCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Roster board {request.RosterBoardCtrlNbr} not found."));

        var ordering = request.Entries
            .Select(e => (ControlNumber.Create(e.PositionCtrlNbr), e.PositionOrder))
            .ToList();

        board.ReorderPositions(ordering);
        await _rosterBoardRepository.UpdateAsync(board);
        var craftName = await ResolveCraftNameAsync(board.CraftCtrlNbr);
        return await MapToResponseAsync(board, craftName);
    }

    private async Task<string> ResolveCraftNameAsync(ControlNumber craftCtrlNbr)
    {
        var craft = await _craftRepository.GetByCtrlNbrAsync(craftCtrlNbr);
        return craft?.CraftName ?? string.Empty;
    }

    private async Task<RosterBoardResponse> MapToResponseAsync(RosterBoard board, string craftName = "")
    {
        var rosterName = string.Empty;
        long workAreaCtrlNbr = 0;
        var workAreaName = string.Empty;
        if (board.RosterCtrlNbr is not null)
        {
            var roster = await _rosterRepository.GetByCtrlNbrAsync(board.RosterCtrlNbr);
            rosterName = roster?.RosterName ?? string.Empty;
            workAreaCtrlNbr = roster?.WorkAreaGroupCtrlNbr.Value ?? 0;
            if (roster?.WorkAreaGroupCtrlNbr is not null)
            {
                var group = await _dynamicGroupRepository.GetByCtrlNbrAsync(roster.WorkAreaGroupCtrlNbr);
                workAreaName = group?.Name ?? string.Empty;
            }
        }

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
        {
            response.Positions.Add(await MapPositionResponseAsync(position));
        }

        return response;
    }

    private async Task<RosterBoardPositionResponse> MapPositionResponseAsync(RosterBoardPosition position)
    {
        var emp = await _employeeRepository.GetByCtrlNbrAsync(position.EmployeeCtrlNbr);
        return new RosterBoardPositionResponse
        {
            CtrlNbr = position.CtrlNbr?.Value ?? 0,
            EmployeeCtrlNbr = position.EmployeeCtrlNbr?.Value ?? 0,
            PositionOrder = position.PositionOrder,
            HangoutStatus = position.HangoutStatus,
            HangoutAt = position.HangoutAtUtc.HasValue
                ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(position.HangoutAtUtc.Value, DateTimeKind.Utc))
                : null,
            EmployeeNumber = emp?.EmployeeNumber ?? string.Empty,
            EmployeeUserId = emp?.UserId ?? string.Empty,
            EmployeeFullNameLnf = await GetEmployeeNameAsync(emp?.UserId)
        };
    }

    private async Task<string> GetEmployeeNameAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId)) return string.Empty;
        var user = await _userManager.FindByIdAsync(userId);
        return user?.FullNameLNF ?? string.Empty;
    }
}