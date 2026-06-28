using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Boards;

public interface IBoardCascadePolicyRepository : IRepository<BoardCascadePolicy>
{
    Task<BoardCascadePolicy?> GetByWorkAreaAndCraftAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr);
}

public interface IRosterBoardRepository : IRepository<RosterBoard>
{
    Task<IReadOnlyList<RosterBoard>> GetActiveByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
    Task<IReadOnlyList<RosterBoard>> GetByCraftCtrlNbrAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default);
    Task<IReadOnlyList<RosterBoard>> GetByCraftCtrlNbrsAsync(IEnumerable<ControlNumber> craftCtrlNbrs, CancellationToken ct = default);
    /// <summary>Returns the board that contains the given <see cref="RosterBoardPosition.CtrlNbr"/>.</summary>
    Task<RosterBoard?> GetByPositionCtrlNbrAsync(ControlNumber positionCtrlNbr, CancellationToken ct = default);
    /// <summary>Returns the board that contains a position backed by the given <see cref="RosterBoardPosition.StaffablePositionCtrlNbr"/>.</summary>
    Task<RosterBoard?> GetByStaffablePositionCtrlNbrAsync(ControlNumber staffablePositionCtrlNbr, CancellationToken ct = default);
}
