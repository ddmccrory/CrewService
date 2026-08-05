using CrewService.Application.Absence;
using CrewService.Application.DailyOperations;
using CrewService.Application.TenantConfig;
using CrewService.Application.Time;
using CrewService.Application.VacancyAssignment;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;

namespace CrewService.UnitTests.Fixtures;

internal static class TestCallSheetVacancyProjectionSyncFactory
{
    public static CallSheetVacancyProjectionSyncService Create(IOrchestrationUnitOfWorkFactory uowFactory)
    {
        var clock = new WorkAreaClock(TimeProvider.System, uowFactory);
        var vacancyEvaluation = new CallSheetSlotVacancyEvaluationService(
            clock,
            new TestRailroadResolver(),
            new NullAbsenceCodeRepository());

        var orchestrator = new VacancyProjectionOrchestratorService(
            new EmptyBoardCandidateProvider(),
            new AlwaysRestedSkipContextProvider());

        return new CallSheetVacancyProjectionSyncService(vacancyEvaluation, orchestrator);
    }

    private sealed class EmptyBoardCandidateProvider : IBoardCandidateProvider
    {
        public Task<IReadOnlyList<SkipRuleCandidate>> GetCandidatesAsync(
            ControlNumber workAreaGroupCtrlNbr,
            ControlNumber craftCtrlNbr,
            SkipRuleSlot slot,
            CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SkipRuleCandidate>>([]);
    }

    private sealed class AlwaysRestedSkipContextProvider : ISkipContextProvider
    {
        public Task<SkipContext> BuildAsync(IOrchestrationUnitOfWork uow, SkipRuleCandidate candidate, SkipRuleSlot slot, CancellationToken ct = default)
            => Task.FromResult(new SkipContext { IsRested = true, IsQualified = true });

        public Task<SkipContext> BuildAsync(SkipRuleCandidate candidate, SkipRuleSlot slot, CancellationToken ct = default)
            => Task.FromResult(new SkipContext { IsRested = true, IsQualified = true });
    }

    private sealed class TestRailroadResolver : IRailroadResolver
    {
        public Task<ControlNumber?> ResolveFromWorkAreaAsync(
            IOrchestrationUnitOfWork uow,
            ControlNumber workAreaGroupCtrlNbr,
            CancellationToken ct = default)
            => Task.FromResult<ControlNumber?>(null);

        public ControlNumber? ResolveFromGroup(DynamicGroup? group)
            => null;
    }

    private sealed class NullAbsenceCodeRepository : IAbsenceCodeRepository
    {
        public Task<List<AbsenceCode>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(new List<AbsenceCode>());

        public Task<AbsenceCodeCraftOverride?> GetOverrideAsync(ControlNumber absenceCodeCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<AbsenceCodeCraftOverride?>(null);

        public Task<List<AbsenceCode>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(new List<AbsenceCode>());

        public Task<List<AbsenceCode>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new List<AbsenceCode>());

        public Task<AbsenceCode?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult<AbsenceCode?>(null);

        public Task<AbsenceCode?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult<AbsenceCode?>(null);

        public Task AddAsync(AbsenceCode entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(AbsenceCode entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public void Add(AbsenceCode entity) { }
        public void Update(AbsenceCode entity) { }
        public void Remove(AbsenceCode entity) { }
    }
}
