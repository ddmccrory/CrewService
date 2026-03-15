using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class BoardsService(
    IExtraBoardRepository boardRepository,
    IBoardMemberRepository memberRepository) : BoardsSrvc.BoardsSrvcBase
{
    public override async Task<GetAllBoardsResponse> GetAllBoards(GetAllBoardsRequest request, ServerCallContext context)
    {
        var boards = string.IsNullOrEmpty(request.BoardKind)
            ? await boardRepository.GetByCraftAsync(ControlNumber.Create(request.CraftCtrlNbr))
            : await boardRepository.GetByKindAsync(request.BoardKind);
        var response = new GetAllBoardsResponse { TotalCount = boards.Count };
        foreach (var b in boards) response.Boards.Add(MapBoard(b));
        return response;
    }

    public override async Task<BoardResponse> GetBoard(GetBoardRequest request, ServerCallContext context)
    {
        var board = await boardRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Board {request.CtrlNbr} not found."));
        return MapBoard(board);
    }

    public override async Task<BoardResponse> CreateBoard(CreateBoardRequest request, ServerCallContext context)
    {
        var board = ExtraBoard.Create(request.CraftCtrlNbr, request.PlacedGroupCtrlNbr, request.BoardKind, request.Name, request.IsActive, request.AuxBoardType);
        await boardRepository.AddAsync(board);
        return MapBoard(board);
    }

    public override async Task<DeleteResponse> DeleteBoard(DeleteBoardRequest request, ServerCallContext context)
    {
        await boardRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
        return new DeleteResponse { Success = true };
    }

    public override async Task<BoardResponse> UpdateBoard(UpdateBoardRequest request, ServerCallContext context)
    {
        var board = await boardRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Board {request.CtrlNbr} not found."));
        board.Update(request.Name, request.IsActive, request.AuxBoardType);
        await boardRepository.UpdateAsync(board);
        return MapBoard(board);
    }

    public override async Task<GetBoardMembersResponse> GetBoardMembers(GetBoardMembersRequest request, ServerCallContext context)
    {
        var members = await memberRepository.GetByBoardAsync(ControlNumber.Create(request.ExtraBoardCtrlNbr));
        var response = new GetBoardMembersResponse { TotalCount = members.Count };
        foreach (var m in members) response.Members.Add(MapMember(m));
        return response;
    }

    public override async Task<BoardMemberResponse> CreateBoardMember(CreateBoardMemberRequest request, ServerCallContext context)
    {
        var startUtc = DateTime.Parse(request.StartUtc).ToUniversalTime();
        DateTime? endUtc = string.IsNullOrEmpty(request.EndUtc) ? null : DateTime.Parse(request.EndUtc).ToUniversalTime();
        var member = BoardMember.Create(request.ExtraBoardCtrlNbr, request.EmployeeCtrlNbr, request.OrderIndex, startUtc, endUtc);
        await memberRepository.AddAsync(member);
        return MapMember(member);
    }

    private static BoardResponse MapBoard(ExtraBoard b) => new()
    {
        CtrlNbr = b.CtrlNbr.Value,
        CraftCtrlNbr = b.CraftCtrlNbr.Value,
        PlacedGroupCtrlNbr = b.PlacedGroupCtrlNbr.Value,
        BoardKind = b.BoardKind,
        Name = b.Name,
        IsActive = b.IsActive,
        AuxBoardType = b.AuxBoardType ?? string.Empty
    };

    private static BoardMemberResponse MapMember(BoardMember m) => new()
    {
        CtrlNbr = m.CtrlNbr.Value,
        ExtraBoardCtrlNbr = m.ExtraBoardCtrlNbr.Value,
        EmployeeCtrlNbr = m.EmployeeCtrlNbr.Value,
        OrderIndex = m.OrderIndex,
        StateJson = m.StateJson ?? string.Empty,
        StartUtc = m.StartUtc.ToString("O"),
        EndUtc = m.EndUtc?.ToString("O") ?? string.Empty
    };
}
