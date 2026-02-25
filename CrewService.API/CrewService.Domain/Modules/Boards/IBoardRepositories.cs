using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Boards;

public interface IExtraBoardRepository : IRepository<ExtraBoard>
{
    Task<List<ExtraBoard>> GetByCraftAsync(ControlNumber craftCtrlNbr);
    Task<List<ExtraBoard>> GetByPlacedGroupAsync(ControlNumber placedGroupCtrlNbr);
    Task<List<ExtraBoard>> GetByKindAsync(string boardKind);
}

public interface IBoardMemberRepository : IRepository<BoardMember>
{
    Task<List<BoardMember>> GetByBoardAsync(ControlNumber extraBoardCtrlNbr);
    Task<List<BoardMember>> GetActiveByBoardAsync(ControlNumber extraBoardCtrlNbr, DateTime asOfUtc);
}

public interface IBoardCascadePolicyRepository : IRepository<BoardCascadePolicy>
{
    Task<BoardCascadePolicy?> GetByWorkAreaAndCraftAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr);
}
