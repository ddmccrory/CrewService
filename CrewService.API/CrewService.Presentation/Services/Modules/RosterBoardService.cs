using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class RosterBoardService()
    : RosterBoardSrvc.RosterBoardSrvcBase
{
    public override Task<RosterBoardResponse> GetRosterBoard(
        GetRosterBoardRequest request, ServerCallContext context)
    {
        return Task.FromResult(new RosterBoardResponse
        {
            CtrlNbr = request.CtrlNbr,
            BoardType = string.Empty,
            RotationType = string.Empty,
        });
    }

    public override Task<RosterBoardPositionResponse> HangoutPosition(
        HangoutPositionRequest request, ServerCallContext context)
    {
        return Task.FromResult(new RosterBoardPositionResponse
        {
            CtrlNbr = request.PositionCtrlNbr,
            HangoutStatus = "HungOut",
        });
    }

    public override Task<RosterBoardPositionResponse> RestorePosition(
        RestorePositionRequest request, ServerCallContext context)
    {
        return Task.FromResult(new RosterBoardPositionResponse
        {
            CtrlNbr = request.PositionCtrlNbr,
            HangoutStatus = "Active",
        });
    }
}
