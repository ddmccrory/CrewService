using CrewService.Application.DailyOperations;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.DailyOperations;

public class CallSheetGenerationServiceTests
{
    private sealed class FakeAssignmentQueryService(IReadOnlyList<AssignmentDto> templates) : IAssignmentQueryService
    {
        public Task<IReadOnlyList<AssignmentDto>> GetTemplatesForDateAsync(
            ControlNumber workAreaGroupCtrlNbr, ControlNumber shiftDefinitionCtrlNbr,
            DateOnly targetDate, ControlNumber? departmentCtrlNbr = null, CancellationToken ct = default)
            => Task.FromResult(templates);
    }

    private sealed class FakeShiftDefinitionRepository(ShiftDefinition? shiftDef) : FakeRepository<ShiftDefinition>, IShiftDefinitionRepository
    {
        public override Task<ShiftDefinition?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(shiftDef);

        public Task<List<ShiftDefinition>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr)
            => Task.FromResult(new List<ShiftDefinition>());
    }

    private sealed class FakeShiftInstanceRepository : IShiftInstanceRepository
    {
        public readonly List<ShiftInstance> Added = [];

        public Task AddAsync(ShiftInstance instance, CancellationToken ct = default)
        {
            Added.Add(instance);
            return Task.CompletedTask;
        }

        public Task<ShiftInstance?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult<ShiftInstance?>(null);

        public Task<IReadOnlyList<ShiftInstance>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ShiftInstance>>([]);

        public Task<bool> ExistsByWorkInstanceAndShiftCodeAsync(ControlNumber workInstanceCtrlNbr, string shiftCode, ControlNumber? departmentCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeWorkInstanceRepository : FakeRepository<WorkInstance>, IWorkInstanceRepository
    {
        public readonly List<WorkInstance> Added = [];

        public override Task AddAsync(WorkInstance entity, CancellationToken ct = default)
        {
            Added.Add(entity);
            return Task.CompletedTask;
        }

        public Task<List<WorkInstance>> GetByWorkAreaAndDateRangeAsync(
            ControlNumber workAreaGroupCtrlNbr, DateTime startUtc, DateTime endUtc)
            => Task.FromResult(new List<WorkInstance>());
    }

    private sealed class FakeDepartmentRepository : FakeRepository<Department>, IDepartmentRepository
    {
        public Task<List<Department>> GetByParentAndRailroadAsync(ControlNumber? parentCtrlNbr, ControlNumber? railroadCtrlNbr)
            => Task.FromResult(new List<Department>());
    }

    /// <summary>
    /// Minimal fake for <see cref="IRepository{TEntity}"/>. Override specific members as needed.
    /// </summary>
    private abstract class FakeRepository<TEntity> : IRepository<TEntity> where TEntity : Entity
    {
        public virtual Task<List<TEntity>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<TEntity>());
        public virtual Task<List<TEntity>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<TEntity>());
        public virtual Task<TEntity?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<TEntity?>(default);
        public virtual Task<TEntity?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<TEntity?>(default);
        public virtual Task AddAsync(TEntity entity, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task UpdateAsync(TEntity entity, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public virtual void Add(TEntity entity) { }
        public virtual void Update(TEntity entity) { }
        public virtual void Remove(TEntity entity) { }
    }

    private static ShiftDefinition CreateActiveShiftDef(long workAreaCtrlNbr = 1)
    {
        return ShiftDefinition.Create(
            ControlNumber.Create(workAreaCtrlNbr),
            "1", "First Shift", 1, isActive: true);
    }

    [Fact]
    public async Task GenerateForShift_NoTemplatesOnDate_CreatesEmptyShiftInstance()
    {
        // Arrange - Saturday, no assignments scheduled
        var shiftDef = CreateActiveShiftDef();
        var shiftInstanceRepo = new FakeShiftInstanceRepository();

        var sut = new CallSheetGenerationService(
            new FakeAssignmentQueryService([]),
            new FakeShiftDefinitionRepository(shiftDef),
            shiftInstanceRepo,
            new FakeWorkInstanceRepository(),
            new FakeDepartmentRepository());

        // Act - should NOT throw
        var result = await sut.GenerateForShiftAsync(
            ControlNumber.Create(1),
            shiftDef.CtrlNbr,
            new DateOnly(2026, 4, 4),  // Saturday
            ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.ShiftCode);
        Assert.Empty(result.PositionSlots);
        Assert.Single(shiftInstanceRepo.Added);
    }

    [Fact]
    public async Task GenerateForShift_WithTemplates_CreatesPositionSlots()
    {
        // Arrange - weekday with one assignment having two positions
        var shiftDef = CreateActiveShiftDef();
        var shiftInstanceRepo = new FakeShiftInstanceRepository();

        var templates = new List<AssignmentDto>
        {
            new(ControlNumber.Create(130), ControlNumber.Create(1), null,
                "TY-101", "Pool Turn 101",
                new TimeOnly(7, 0), new TimeOnly(15, 0),
                "Test Group", "TG",
                [
                    new CrewPositionDto(ControlNumber.Create(10), ControlNumber.Create(200), 1, "Engineer"),
                    new CrewPositionDto(ControlNumber.Create(11), null, 2, "Conductor")
                ])
        };

        var sut = new CallSheetGenerationService(
            new FakeAssignmentQueryService(templates),
            new FakeShiftDefinitionRepository(shiftDef),
            shiftInstanceRepo,
            new FakeWorkInstanceRepository(),
            new FakeDepartmentRepository());

        // Act
        var result = await sut.GenerateForShiftAsync(
            ControlNumber.Create(1),
            shiftDef.CtrlNbr,
            new DateOnly(2026, 4, 6),  // Monday
            ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.PositionSlots.Count);
        Assert.Single(shiftInstanceRepo.Added);
    }

    [Fact]
    public async Task GenerateForShift_InactiveShift_Throws()
    {
        var shiftDef = ShiftDefinition.Create(
            ControlNumber.Create(1), "1", "First Shift", 1, isActive: false);

        var sut = new CallSheetGenerationService(
            new FakeAssignmentQueryService([]),
            new FakeShiftDefinitionRepository(shiftDef),
            new FakeShiftInstanceRepository(),
            new FakeWorkInstanceRepository(),
            new FakeDepartmentRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GenerateForShiftAsync(
                ControlNumber.Create(1), shiftDef.CtrlNbr,
                new DateOnly(2026, 4, 6),
                ct: TestContext.Current.CancellationToken));
    }
}
