using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;

namespace CrewService.UnitTests.Fixtures;

internal sealed class NoOpSeniorityMoveRepository : ISeniorityMoveRepository
{
    public Task<List<SeniorityMove>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default) => Task.FromResult<List<SeniorityMove>>([]);
    public Task<List<SeniorityMove>> GetByCraftAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default) => Task.FromResult<List<SeniorityMove>>([]);
    public Task<List<SeniorityMove>> GetByStatusAsync(string status, CancellationToken ct = default) => Task.FromResult<List<SeniorityMove>>([]);
    public Task<List<SeniorityMove>> GetByCraftByStatusAsync(ControlNumber craftCtrlNbr, string status, CancellationToken ct = default) => Task.FromResult<List<SeniorityMove>>([]);
    public Task<List<SeniorityMove>> GetPendingAsync(CancellationToken ct = default) => Task.FromResult<List<SeniorityMove>>([]);
    public Task<List<SeniorityMove>> GetActiveAsync(CancellationToken ct = default) => Task.FromResult<List<SeniorityMove>>([]);
    public Task<List<SeniorityMove>> GetAllMovesAsync(CancellationToken ct = default) => Task.FromResult<List<SeniorityMove>>([]);
    public Task<List<SeniorityMove>> GetApprovedDueAsync(DateTime asOf, CancellationToken ct = default) => Task.FromResult<List<SeniorityMove>>([]);
    public Task<DateTime?> GetNextApprovedEffectiveUtcAsync(CancellationToken ct = default) => Task.FromResult<DateTime?>(null);
    public Task<List<SeniorityMove>> GetPendingByTargetPositionAsync(ControlNumber targetPositionCtrlNbr, ControlNumber excludeCtrlNbr, CancellationToken ct = default) => Task.FromResult<List<SeniorityMove>>([]);

    public Task<List<SeniorityMove>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<List<SeniorityMove>>([]);
    public Task<List<SeniorityMove>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult<List<SeniorityMove>>([]);
    public Task<SeniorityMove?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<SeniorityMove?>(null);
    public Task<SeniorityMove?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<SeniorityMove?>(null);
    public Task AddAsync(SeniorityMove entity, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateAsync(SeniorityMove entity, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
    public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
    public void Add(SeniorityMove entity) { }
    public void Update(SeniorityMove entity) { }
    public void Remove(SeniorityMove entity) { }
}
