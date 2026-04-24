using CrewService.Domain.Interfaces;
using CrewService.Presentation.Services;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class RosterBoardService(
    IRosterBoardRepository rosterBoardRepository,
    IRosterRepository rosterRepository,
    IEmployeeRepository employeeRepository,
    IDynamicGroupRepository dynamicGroupRepository,
    ICraftRepository craftRepository,
    IPositionAssignmentRepository positionAssignmentRepository,
    IQualificationTypeRepository qualificationTypeRepository,
    IEmployeeQualificationRepository employeeQualificationRepository,
    IOrchestrationUnitOfWorkFactory uowFactory,
    EmployeeNameService employeeNameService)
    : RosterBoardSrvc.RosterBoardSrvcBase
{
    private readonly IRosterBoardRepository _rosterBoardRepository = rosterBoardRepository;
    private readonly IRosterRepository _rosterRepository = rosterRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IDynamicGroupRepository _dynamicGroupRepository = dynamicGroupRepository;
    private readonly ICraftRepository _craftRepository = craftRepository;
    private readonly IPositionAssignmentRepository _positionAssignmentRepository = positionAssignmentRepository;
    private readonly IQualificationTypeRepository _qualificationTypeRepository = qualificationTypeRepository;
    private readonly IEmployeeQualificationRepository _employeeQualificationRepository = employeeQualificationRepository;
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

        var employeeCtrlNbr = ControlNumber.Create(request.EmployeeCtrlNbr);

        // Cross-cutting guard: covers crew incumbencies AND board positions
        var existingAssignments = await _positionAssignmentRepository.GetByEmployeeAsync(employeeCtrlNbr);
        if (existingAssignments.Count > 0)
            throw new RpcException(new Status(StatusCode.AlreadyExists,
                "This employee is already assigned to a staffable position. Unassign them first."));

        var staffablePosition = StaffablePosition.Create("Board");
        var position = board.AddPosition(employeeCtrlNbr, request.PositionOrder, staffablePosition.CtrlNbr);
        var positionAssignment = PositionAssignment.Create(
            staffablePosition.CtrlNbr, employeeCtrlNbr, "Board",
            position.CtrlNbr);

        await using var uow = await uowFactory.CreateAsync();
        uow.StaffablePositions.Add(staffablePosition);
        uow.PositionAssignments.Add(positionAssignment);
        uow.RosterBoards.Update(board);
        await uow.CommitAsync();

        return await MapPositionResponseAsync(position);
    }

    public override async Task<DeleteResponse> RemoveRosterBoardPosition(
        RemoveRosterBoardPositionRequest request, ServerCallContext context)
    {
        await using var uow = await uowFactory.CreateAsync();

        var boards = await uow.RosterBoards.GetAllAsync();
        var board = boards.FirstOrDefault(b => b.Positions.Any(p => p.CtrlNbr == ControlNumber.Create(request.CtrlNbr)))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Position {request.CtrlNbr} not found on any board."));

        var position = board.Positions.First(p => p.CtrlNbr == ControlNumber.Create(request.CtrlNbr));
        var positionAssignment = await uow.PositionAssignments.GetByStaffablePositionAsync(position.StaffablePositionCtrlNbr);

        board.RemovePosition(position);
        uow.RosterBoards.Update(board);
        if (positionAssignment is not null)
            uow.PositionAssignments.Remove(positionAssignment);
        await uow.CommitAsync();

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
            EmployeeFullNameLnf = await employeeNameService.GetFullNameLnfAsync(emp?.UserId)
        };
    }

    public override async Task<GetEligibleEmployeesForRosterBoardResponse> GetEligibleEmployeesForRosterBoard(
        GetEligibleEmployeesForRosterBoardRequest request, ServerCallContext context)
    {
        var craftCtrlNbr = ControlNumber.Create(request.CraftCtrlNbr);
        var clientCtrlNbr = ControlNumber.Create(request.ClientCtrlNbr);

        // All qual types scoped to this craft
        var craftQualTypes = await _qualificationTypeRepository.GetActiveByCraftCtrlNbrAsync(craftCtrlNbr);
        var craftQualTypeCtrlNbrs = craftQualTypes.Select(q => q.CtrlNbr).ToHashSet();

        // No qual types defined for this craft — no one is eligible
        if (craftQualTypeCtrlNbrs.Count == 0)
            return new GetEligibleEmployeesForRosterBoardResponse();

        // All employees for this railroad, excluding those already assigned to any staffable position
        var employees = await _employeeRepository.GetListByClientCtrlNbrAsync(clientCtrlNbr);
        if (employees.Count == 0)
            return new GetEligibleEmployeesForRosterBoardResponse();

        var assignedCtrlNbrs = await _positionAssignmentRepository.GetAssignedEmployeeCtrlNbrsAsync();
        var unassigned = assignedCtrlNbrs.Count == 0
            ? employees
            : employees.Where(e => !assignedCtrlNbrs.Contains(e.CtrlNbr.Value)).ToList();

        // Filter to employees who hold at least one active qualification in this craft
        var empQuals = await _employeeQualificationRepository.GetActiveByEmployeeCtrlNbrsAsync(unassigned.Select(e => e.CtrlNbr));
        var qualifiedCtrlNbrs = empQuals
            .Where(eq => craftQualTypeCtrlNbrs.Contains(eq.QualificationTypeCtrlNbr))
            .Select(eq => eq.EmployeeCtrlNbr)
            .ToHashSet();

        var qualified = unassigned.Where(e => qualifiedCtrlNbrs.Contains(e.CtrlNbr)).ToList();
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

}