using CrewService.Application.VacancyAssignment;
using CrewService.Application.VacancyAssignment.Rules;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.VacancyAssignment;

public class SkipRuleTests
{
    private static SkipRuleCandidate MakeCandidate() => new(ControlNumber.Create(1), ControlNumber.Create(10), 1);
    private static SkipRuleSlot MakeSlot() => new(ControlNumber.Create(100), ControlNumber.Create(200));

    [Fact]
    public void WorkedCapRule_UnderCap_NoSkip()
    {
        var rule = new WorkedCapRule();
        var ctx = new SkipContext { RecentOnDutyCount = 10, WorkedDayCap = 12 };
        Assert.False(rule.ShouldSkip(MakeCandidate(), MakeSlot(), ctx));
    }

public class VacancyResolutionEngineTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNotQualified_LogsSkipDecisionWithBlockingReasons()
    {
        var slot = new SkipRuleSlot(ControlNumber.Create(100), ControlNumber.Create(200));
        var candidate = new SkipRuleCandidate(ControlNumber.Create(1), ControlNumber.Create(10), 1);

        var decisionLogRepo = new FakeDispatchDecisionLogRepository();
        var runRepo = new FakeVacancyResolutionRunRepository();

        var engine = new VacancyResolutionEngine(
            new FakeOpenSlotProvider([slot]),
            new FakeBoardCandidateProvider([candidate]),
            new FakeSkipContextProvider(new SkipContext
            {
                IsQualified = false,
                QualificationBlockingReasons = ["NOT_QUALIFIED: Missing FOREMAN qualification"],
                IsRested = true
            }),
            runRepo,
            decisionLogRepo,
            [new QualificationRule()],
            new StandardAssignmentStrategy());

        await engine.ExecuteAsync(
            ControlNumber.Create(500),
            ControlNumber.Create(600),
            ControlNumber.Create(700),
            TestContext.Current.CancellationToken);

        var skipLog = Assert.Single(decisionLogRepo.AddedLogs.Where(l => l.Phase == "Skip"));
        Assert.Contains("NOT_QUALIFIED", skipLog.DecisionJson);
        Assert.Contains("Missing FOREMAN qualification", skipLog.DecisionJson);
    }

    private sealed class FakeOpenSlotProvider(IReadOnlyList<SkipRuleSlot> slots) : IOpenSlotProvider
    {
        public Task<IReadOnlyList<SkipRuleSlot>> GetOpenSlotsAsync(ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(slots);
    }

    private sealed class FakeBoardCandidateProvider(IReadOnlyList<SkipRuleCandidate> candidates) : IBoardCandidateProvider
    {
        public Task<IReadOnlyList<SkipRuleCandidate>> GetCandidatesAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(candidates);
    }

    private sealed class FakeSkipContextProvider(SkipContext ctx) : ISkipContextProvider
    {
        public Task<SkipContext> BuildAsync(SkipRuleCandidate candidate, SkipRuleSlot slot, CancellationToken ct = default)
            => Task.FromResult(ctx);
    }

    private sealed class FakeVacancyResolutionRunRepository : IVacancyResolutionRunRepository
    {
        public List<VacancyResolutionRun> AddedRuns { get; } = [];

        public Task AddAsync(VacancyResolutionRun run, CancellationToken ct = default)
        {
            AddedRuns.Add(run);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDispatchDecisionLogRepository : FakeRepositoryBase<DispatchDecisionLog>, IDispatchDecisionLogRepository
    {
        public List<DispatchDecisionLog> AddedLogs { get; } = [];

        public override Task AddAsync(DispatchDecisionLog entity, CancellationToken ct = default)
        {
            AddedLogs.Add(entity);
            return Task.CompletedTask;
        }

        public Task<List<DispatchDecisionLog>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr)
            => Task.FromResult(AddedLogs.Where(l => l.PositionSlotCtrlNbr == positionSlotCtrlNbr).ToList());
    }

    private abstract class FakeRepositoryBase<TEntity> : IRepository<TEntity> where TEntity : Entity
    {
        public virtual Task<List<TEntity>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<TEntity>());
        public virtual Task<List<TEntity>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<TEntity>());
        public virtual Task<TEntity?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<TEntity?>(null);
        public virtual Task<TEntity?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<TEntity?>(null);
        public virtual Task AddAsync(TEntity entity, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task UpdateAsync(TEntity entity, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public virtual void Add(TEntity entity) { }
        public virtual void Update(TEntity entity) { }
        public virtual void Remove(TEntity entity) { }
    }
}

    [Fact]
    public void WorkedCapRule_AtCap_Skips()
    {
        var rule = new WorkedCapRule();
        var ctx = new SkipContext { RecentOnDutyCount = 12, WorkedDayCap = 12 };
        Assert.True(rule.ShouldSkip(MakeCandidate(), MakeSlot(), ctx));
    }

    [Fact]
    public void AlreadyOnDutyRule_OnDuty_Skips()
    {
        var rule = new AlreadyOnDutyRule();
        var ctx = new SkipContext { HasActiveOnDuty = true };
        Assert.True(rule.ShouldSkip(MakeCandidate(), MakeSlot(), ctx));
    }

    [Fact]
    public void AlreadyOnDutyRule_NotOnDuty_NoSkip()
    {
        var rule = new AlreadyOnDutyRule();
        var ctx = new SkipContext { HasActiveOnDuty = false };
        Assert.False(rule.ShouldSkip(MakeCandidate(), MakeSlot(), ctx));
    }

    [Fact]
    public void AvailabilityRule_NotYetRested_Skips()
    {
        var rule = new AvailabilityRule();
        var now = DateTime.UtcNow;
        var ctx = new SkipContext { NowUtc = now, RestedAtUtc = now.AddHours(1) };
        Assert.True(rule.ShouldSkip(MakeCandidate(), MakeSlot(), ctx));
    }

    [Fact]
    public void AvailabilityRule_AlreadyRested_NoSkip()
    {
        var rule = new AvailabilityRule();
        var now = DateTime.UtcNow;
        var ctx = new SkipContext { NowUtc = now, RestedAtUtc = now.AddHours(-1) };
        Assert.False(rule.ShouldSkip(MakeCandidate(), MakeSlot(), ctx));
    }

    [Fact]
    public void RestRule_NotRested_Skips()
    {
        var rule = new RestRule();
        var ctx = new SkipContext { IsRested = false };
        Assert.True(rule.ShouldSkip(MakeCandidate(), MakeSlot(), ctx));
    }

    [Fact]
    public void MarkOffRule_MarkedOff_Skips()
    {
        var rule = new MarkOffRule();
        var ctx = new SkipContext { IsMarkedOff = true };
        Assert.True(rule.ShouldSkip(MakeCandidate(), MakeSlot(), ctx));
    }

    [Fact]
    public void QualificationRule_NotQualified_Skips()
    {
        var rule = new QualificationRule();
        var ctx = new SkipContext { IsQualified = false };
        Assert.True(rule.ShouldSkip(MakeCandidate(), MakeSlot(), ctx));
    }

    [Fact]
    public void WeeklyHoursCapRule_UnderCap_NoSkip()
    {
        var rule = new WeeklyHoursCapRule();
        var ctx = new SkipContext { WeeklyHoursWorked = 30, WeeklyHoursCap = 40 };
        Assert.False(rule.ShouldSkip(MakeCandidate(), MakeSlot(), ctx));
    }

    [Fact]
    public void WeeklyHoursCapRule_AtCap_Skips()
    {
        var rule = new WeeklyHoursCapRule();
        var ctx = new SkipContext { WeeklyHoursWorked = 40, WeeklyHoursCap = 40 };
        Assert.True(rule.ShouldSkip(MakeCandidate(), MakeSlot(), ctx));
    }
}

public class AssignmentStrategyTests
{
    [Fact]
    public void StandardStrategy_AlwaysSucceeds()
    {
        var strategy = new StandardAssignmentStrategy();
        var candidate = new SkipRuleCandidate(ControlNumber.Create(1), ControlNumber.Create(10), 1);
        var slot = new SkipRuleSlot(ControlNumber.Create(100), ControlNumber.Create(200));
        var result = strategy.TryAssign(candidate, slot, new AssignmentContext());
        Assert.True(result.Success);
        Assert.Equal(candidate.EmployeeCtrlNbr, result.AssignedEmployeeCtrlNbr);
    }

    [Fact]
    public void ForemanHelperStrategy_Disabled_Fails()
    {
        var strategy = new ForemanHelperStrategy();
        var candidate = new SkipRuleCandidate(ControlNumber.Create(1), ControlNumber.Create(10), 1);
        var slot = new SkipRuleSlot(ControlNumber.Create(100), ControlNumber.Create(200));
        var result = strategy.TryAssign(candidate, slot, new AssignmentContext { HelperSearchEnabled = false });
        Assert.False(result.Success);
    }

    [Fact]
    public void ForemanHelperStrategy_Enabled_Succeeds()
    {
        var strategy = new ForemanHelperStrategy();
        var candidate = new SkipRuleCandidate(ControlNumber.Create(1), ControlNumber.Create(10), 1);
        var slot = new SkipRuleSlot(ControlNumber.Create(100), ControlNumber.Create(200));
        var result = strategy.TryAssign(candidate, slot, new AssignmentContext { HelperSearchEnabled = true });
        Assert.True(result.Success);
    }
}

public class VacancyResolutionRunTests
{
    [Fact]
    public void Start_SetsRunningStatus()
    {
        var run = VacancyResolutionRun.Start(ControlNumber.Create(1), ControlNumber.Create(2));
        Assert.Equal("Running", run.Status);
        Assert.Null(run.CompletedAtUtc);
    }

    [Fact]
    public void Complete_SetsCountsAndStatus()
    {
        var run = VacancyResolutionRun.Start(ControlNumber.Create(1), ControlNumber.Create(2));
        run.Complete(5, 3);
        Assert.Equal("Completed", run.Status);
        Assert.Equal(5, run.SlotsEvaluated);
        Assert.Equal(3, run.SlotsFilled);
        Assert.NotNull(run.CompletedAtUtc);
    }

    [Fact]
    public void Fail_SetsFailedStatus()
    {
        var run = VacancyResolutionRun.Start(ControlNumber.Create(1), ControlNumber.Create(2));
        run.Fail();
        Assert.Equal("Failed", run.Status);
    }
}
