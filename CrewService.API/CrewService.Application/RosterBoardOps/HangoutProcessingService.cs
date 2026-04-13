using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.RosterBoardOps;

public sealed class HangoutProcessingService(IRosterBoardRepository boardRepo)
{
    public async Task<int> ProcessHangoutsAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
    {
        var boards = await boardRepo.GetActiveByWorkAreaAsync(workAreaGroupCtrlNbr, ct);
        var hungOut = 0;

        foreach (var board in boards)
        {
            foreach (var position in board.Positions)
            {
                if (position.HangoutStatus == "Active")
                {
                    position.Hangout();
                    hungOut++;
                }
            }
        }

        return hungOut;
    }
}
