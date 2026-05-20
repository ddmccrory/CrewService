using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Boards;

public interface IRequiredPositionsStrategyRepository : IRepository<RequiredPositionsStrategy>
{
    /// <summary>Returns the system-wide Static strategy (Code == "STATIC").</summary>
    Task<RequiredPositionsStrategy?> GetStaticAsync(CancellationToken ct = default);

    /// <summary>Returns all system-level strategies.</summary>
    Task<List<RequiredPositionsStrategy>> GetAllSystemStrategiesAsync(CancellationToken ct = default);

    Task<RequiredPositionsStrategy?> GetByCodeAsync(string code, CancellationToken ct = default);
}

public interface ICraftRequiredPositionsStrategyRepository : IRepository<CraftRequiredPositionsStrategy>
{
    Task<CraftRequiredPositionsStrategy?> GetByCraftAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default);
    Task<List<CraftRequiredPositionsStrategy>> GetByCraftsAsync(IEnumerable<ControlNumber> craftCtrlNbrs, CancellationToken ct = default);
    Task<List<CraftRequiredPositionsStrategy>> GetByStrategyCtrlNbrsAsync(IEnumerable<ControlNumber> strategyCtrlNbrs, CancellationToken ct = default);
}
