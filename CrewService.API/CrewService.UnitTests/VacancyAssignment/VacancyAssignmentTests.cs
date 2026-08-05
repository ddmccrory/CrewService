using CrewService.Application.VacancyAssignment;
using CrewService.Application.VacancyAssignment.Rules;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.WorkManagement;
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
            new FakeBoardSnapshotSource([]),
            new FakeSkipContextProvider(new SkipContext
            {
                IsQualified = false,
                QualificationBlockingReasons = ["NOT_QUALIFIED: Missing FOREMAN qualification"],
                IsRested = true
            }),
            runRepo,
            decisionLogRepo,
            new FakeBoardSnapshotRepository(),
            new FakeBoardSelectionDecisionRepository(),
            [new QualificationRule()],
            new StandardAssignmentStrategy());

        await engine.ExecuteAsync(
            ControlNumber.Create(500),
            ControlNumber.Create(600),
            ControlNumber.Create(700),
            options: null,
            TestContext.Current.CancellationToken);

        var skipLog = Assert.Single(decisionLogRepo.AddedLogs, l => l.Phase == "Skip");
        Assert.Contains("NOT_QUALIFIED", skipLog.DecisionJson);
        Assert.Contains("Missing FOREMAN qualification", skipLog.DecisionJson);
    }

    [Fact]
    public async Task ExecuteAsync_CapturesOrderedSnapshotRows_AndSelectionDecisionEvidence()
    {
        var shiftCtrlNbr = ControlNumber.Create(600);
        var slot = new SkipRuleSlot(ControlNumber.Create(100), ControlNumber.Create(200));
        var candidate = new SkipRuleCandidate(ControlNumber.Create(1), ControlNumber.Create(10), 1);

        var runRepo = new FakeVacancyResolutionRunRepository();
        var decisionLogRepo = new FakeDispatchDecisionLogRepository();
        var snapshotRepo = new FakeBoardSnapshotRepository();
        var decisionRepo = new FakeBoardSelectionDecisionRepository();
        var boardSlots = new List<BoardSnapshotSlot>
        {
            new(ControlNumber.Create(3003), shiftCtrlNbr, ControlNumber.Create(91), ControlNumber.Create(801), ControlNumber.Create(2), 2, 20, null, "Active", "Extra Board", "Employee B", "Position B"),
            new(ControlNumber.Create(3002), shiftCtrlNbr, ControlNumber.Create(91), ControlNumber.Create(802), ControlNumber.Create(1), 1, 20, null, "Active", "Extra Board", "Employee A", "Position A"),
            new(ControlNumber.Create(3001), shiftCtrlNbr, ControlNumber.Create(91), ControlNumber.Create(803), ControlNumber.Create(3), 1, 10, null, "Active", "Extra Board", "Employee C", "Position C")
        };

        var engine = new VacancyResolutionEngine(
            new FakeOpenSlotProvider([slot]),
            new FakeBoardCandidateProvider([candidate]),
            new FakeBoardSnapshotSource(boardSlots),
            new FakeSkipContextProvider(new SkipContext { IsQualified = true, IsRested = true }),
            runRepo,
            decisionLogRepo,
            snapshotRepo,
            decisionRepo,
            [new QualificationRule()],
            new StandardAssignmentStrategy());

        await engine.ExecuteAsync(
            ControlNumber.Create(500),
            shiftCtrlNbr,
            ControlNumber.Create(700),
            options: VacancyResolutionExecutionOptions.Default,
            TestContext.Current.CancellationToken);

        var snapshot = Assert.Single(snapshotRepo.Snapshots);
        Assert.Equal(1, snapshot.DecisionSequence);
        Assert.Equal(3, snapshot.Rows.Count);

        Assert.Collection(snapshot.Rows,
            row =>
            {
                Assert.Equal(1, row.BoardOrder);
                Assert.Equal(10, row.CallSequence);
                Assert.Equal("Employee C", row.EmployeeName);
            },
            row =>
            {
                Assert.Equal(1, row.BoardOrder);
                Assert.Equal(20, row.CallSequence);
                Assert.Equal("Employee A", row.EmployeeName);
            },
            row =>
            {
                Assert.Equal(2, row.BoardOrder);
                Assert.Equal(20, row.CallSequence);
                Assert.Equal("Employee B", row.EmployeeName);
            });

        var selectDecision = Assert.Single(decisionRepo.Decisions, d => d.DecisionPhase == "Select");
        Assert.Equal(snapshot.CtrlNbr, selectDecision.SnapshotCtrlNbr);
        Assert.Equal(candidate.EmployeeCtrlNbr, selectDecision.SelectedEmployeeCtrlNbr);
        Assert.Equal(ControlNumber.Create(3002), selectDecision.SelectedBoardSlotInstanceCtrlNbr);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSnapshotCaptureDisabled_DoesNotWriteSnapshot_AndDecisionHasNoSnapshotLink()
    {
        var slot = new SkipRuleSlot(ControlNumber.Create(100), ControlNumber.Create(200));
        var candidate = new SkipRuleCandidate(ControlNumber.Create(1), ControlNumber.Create(10), 1);

        var runRepo = new FakeVacancyResolutionRunRepository();
        var decisionLogRepo = new FakeDispatchDecisionLogRepository();
        var snapshotRepo = new FakeBoardSnapshotRepository();
        var decisionRepo = new FakeBoardSelectionDecisionRepository();

        var engine = new VacancyResolutionEngine(
            new FakeOpenSlotProvider([slot]),
            new FakeBoardCandidateProvider([candidate]),
            new FakeBoardSnapshotSource([]),
            new FakeSkipContextProvider(new SkipContext { IsQualified = true, IsRested = true }),
            runRepo,
            decisionLogRepo,
            snapshotRepo,
            decisionRepo,
            [new QualificationRule()],
            new StandardAssignmentStrategy());

        await engine.ExecuteAsync(
            ControlNumber.Create(500),
            ControlNumber.Create(600),
            ControlNumber.Create(700),
            options: new VacancyResolutionExecutionOptions("PostCall", CaptureBoardSnapshots: false),
            TestContext.Current.CancellationToken);

        Assert.Empty(snapshotRepo.Snapshots);
        var decision = Assert.Single(decisionRepo.Decisions);
        Assert.Null(decision.SnapshotCtrlNbr);
        Assert.Equal("PostCall", decision.DecisionSource);
    }

    [Fact]
    public async Task ExecuteAsync_InterleavedShiftRuns_UseIndependentDecisionSequences()
    {
        var openSlot = new FakeOpenSlotProvider([new SkipRuleSlot(ControlNumber.Create(100), ControlNumber.Create(200))]);
        var candidates = new FakeBoardCandidateProvider([new SkipRuleCandidate(ControlNumber.Create(1), ControlNumber.Create(10), 1)]);
        var boardSnapshotSource = new FakeBoardSnapshotSource([
            new(ControlNumber.Create(7001), ControlNumber.Create(6001), ControlNumber.Create(91), ControlNumber.Create(8001), ControlNumber.Create(1), 1, 1, null, "Active", "Extra Board", "Employee A", "Position A")
        ]);

        var runRepo = new FakeVacancyResolutionRunRepository();
        var decisionLogRepo = new FakeDispatchDecisionLogRepository();
        var snapshotRepo = new FakeBoardSnapshotRepository();
        var decisionRepo = new FakeBoardSelectionDecisionRepository();

        var engine = new VacancyResolutionEngine(
            openSlot,
            candidates,
            boardSnapshotSource,
            new FakeSkipContextProvider(new SkipContext { IsQualified = true, IsRested = true }),
            runRepo,
            decisionLogRepo,
            snapshotRepo,
            decisionRepo,
            [new QualificationRule()],
            new StandardAssignmentStrategy());

        var shiftA = ControlNumber.Create(6001);
        var shiftB = ControlNumber.Create(6002);

        await engine.ExecuteAsync(ControlNumber.Create(500), shiftA, ControlNumber.Create(700), options: VacancyResolutionExecutionOptions.Default, TestContext.Current.CancellationToken);
        await engine.ExecuteAsync(ControlNumber.Create(500), shiftB, ControlNumber.Create(700), options: VacancyResolutionExecutionOptions.Default, TestContext.Current.CancellationToken);
        await engine.ExecuteAsync(ControlNumber.Create(500), shiftA, ControlNumber.Create(700), options: VacancyResolutionExecutionOptions.Default, TestContext.Current.CancellationToken);

        var shiftASnapshots = snapshotRepo.Snapshots.Where(s => s.ShiftInstanceCtrlNbr == shiftA).OrderBy(s => s.DecisionSequence).ToList();
        var shiftBSnapshots = snapshotRepo.Snapshots.Where(s => s.ShiftInstanceCtrlNbr == shiftB).OrderBy(s => s.DecisionSequence).ToList();

        Assert.Equal([1, 2], shiftASnapshots.Select(s => s.DecisionSequence));
        Assert.Equal([1], shiftBSnapshots.Select(s => s.DecisionSequence));
    }

    [Fact]
    public async Task ExecuteAsync_RequestsCandidatesPerOpenSlot()
    {
        var slot1 = new SkipRuleSlot(ControlNumber.Create(100), ControlNumber.Create(200));
        var slot2 = new SkipRuleSlot(ControlNumber.Create(101), ControlNumber.Create(201));
        var candidateProvider = new FakeBoardCandidateProvider([new SkipRuleCandidate(ControlNumber.Create(1), ControlNumber.Create(10), 1)]);

        var engine = new VacancyResolutionEngine(
            new FakeOpenSlotProvider([slot1, slot2]),
            candidateProvider,
            new FakeBoardSnapshotSource([]),
            new FakeSkipContextProvider(new SkipContext { IsQualified = true, IsRested = true }),
            new FakeVacancyResolutionRunRepository(),
            new FakeDispatchDecisionLogRepository(),
            new FakeBoardSnapshotRepository(),
            new FakeBoardSelectionDecisionRepository(),
            [new QualificationRule()],
            new StandardAssignmentStrategy());

        await engine.ExecuteAsync(
            ControlNumber.Create(500),
            ControlNumber.Create(600),
            ControlNumber.Create(700),
            options: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(2, candidateProvider.RequestedSlots.Count);
        Assert.Equal(slot1.PositionSlotCtrlNbr, candidateProvider.RequestedSlots[0].PositionSlotCtrlNbr);
        Assert.Equal(slot2.PositionSlotCtrlNbr, candidateProvider.RequestedSlots[1].PositionSlotCtrlNbr);
    }

    private sealed class FakeOpenSlotProvider(IReadOnlyList<SkipRuleSlot> slots) : IOpenSlotProvider
    {
        public Task<IReadOnlyList<SkipRuleSlot>> GetOpenSlotsAsync(ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(slots);
    }

    private sealed class FakeBoardCandidateProvider(IReadOnlyList<SkipRuleCandidate> candidates) : IBoardCandidateProvider
    {
        public List<SkipRuleSlot> RequestedSlots { get; } = [];

        public Task<IReadOnlyList<SkipRuleCandidate>> GetCandidatesAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr, SkipRuleSlot slot, CancellationToken ct = default)
        {
            RequestedSlots.Add(slot);
            return Task.FromResult(candidates);
        }
    }

    private sealed class FakeSkipContextProvider(SkipContext ctx) : ISkipContextProvider
    {
        public Task<SkipContext> BuildAsync(IOrchestrationUnitOfWork uow, SkipRuleCandidate candidate, SkipRuleSlot slot, CancellationToken ct = default)
            => Task.FromResult(ctx);

        public Task<SkipContext> BuildAsync(SkipRuleCandidate candidate, SkipRuleSlot slot, CancellationToken ct = default)
            => Task.FromResult(ctx);
    }

    private sealed class FakeBoardSnapshotSource(IReadOnlyList<BoardSnapshotSlot> slots) : IBoardSnapshotSource
    {
        public Task<IReadOnlyList<BoardSnapshotSlot>> GetBoardSlotsAsync(ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(slots);
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

        public override void Add(DispatchDecisionLog entity)
        {
            AddedLogs.Add(entity);
        }

        public Task<List<DispatchDecisionLog>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr)
            => Task.FromResult(AddedLogs.Where(l => l.PositionSlotCtrlNbr == positionSlotCtrlNbr).ToList());
    }

    private sealed class FakeBoardSnapshotRepository : FakeRepositoryBase<BoardSnapshot>, IBoardSnapshotRepository
    {
        private readonly List<BoardSnapshot> _snapshots = [];

        public IReadOnlyList<BoardSnapshot> Snapshots => _snapshots;

        public Task<IReadOnlyList<BoardSnapshot>> GetByShiftInstanceAsync(ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BoardSnapshot>>(_snapshots.Where(s => s.ShiftInstanceCtrlNbr == shiftInstanceCtrlNbr).ToList());

        public Task<IReadOnlyList<BoardSnapshot>> GetByPositionSlotInstanceAsync(ControlNumber positionSlotInstanceCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BoardSnapshot>>(_snapshots.Where(s => s.PositionSlotInstanceCtrlNbr == positionSlotInstanceCtrlNbr).ToList());

        public Task<int> GetNextDecisionSequenceAsync(ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default)
        {
            var max = _snapshots.Where(s => s.ShiftInstanceCtrlNbr == shiftInstanceCtrlNbr).Select(s => (int?)s.DecisionSequence).Max();
            return Task.FromResult((max ?? 0) + 1);
        }

        public override Task AddAsync(BoardSnapshot entity, CancellationToken ct = default)
        {
            _snapshots.Add(entity);
            return Task.CompletedTask;
        }

        public override void Add(BoardSnapshot entity)
        {
            _snapshots.Add(entity);
        }
    }

    private sealed class FakeBoardSelectionDecisionRepository : FakeRepositoryBase<BoardSelectionDecision>, IBoardSelectionDecisionRepository
    {
        private readonly List<BoardSelectionDecision> _decisions = [];

        public IReadOnlyList<BoardSelectionDecision> Decisions => _decisions;

        public Task<IReadOnlyList<BoardSelectionDecision>> GetByShiftInstanceAsync(ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BoardSelectionDecision>>(_decisions.Where(d => d.ShiftInstanceCtrlNbr == shiftInstanceCtrlNbr).ToList());

        public Task<IReadOnlyList<BoardSelectionDecision>> GetByPositionSlotInstanceAsync(ControlNumber positionSlotInstanceCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BoardSelectionDecision>>(_decisions.Where(d => d.PositionSlotInstanceCtrlNbr == positionSlotInstanceCtrlNbr).ToList());

        public override Task AddAsync(BoardSelectionDecision entity, CancellationToken ct = default)
        {
            _decisions.Add(entity);
            return Task.CompletedTask;
        }

        public override void Add(BoardSelectionDecision entity)
        {
            _decisions.Add(entity);
        }
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
