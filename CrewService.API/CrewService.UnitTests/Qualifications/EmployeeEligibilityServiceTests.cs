using CrewService.Application.Qualifications;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Qualifications;

public sealed class EmployeeEligibilityServiceTests
{
    [Fact]
    public async Task CheckEligibilityAsync_WhenCraftMembershipMissing_ReturnsBlockingReason()
    {
        var employeeCtrlNbr = ControlNumber.Create(10);
        var positionSlotCtrlNbr = ControlNumber.Create(100);
        var craftCtrlNbr = ControlNumber.Create(200);
        var craftRole = CraftRole.Create(craftCtrlNbr, "TRN", "Trainman");

        var slotRequirementRepository = new FakeSlotRequirementRepository([
            SlotRequirement.Create(positionSlotCtrlNbr, 1, craftRoleCtrlNbr: craftRole.CtrlNbr)
        ]);

        var sut = new EmployeeEligibilityService(
            slotRequirementRepository,
            new FakePositionSlotRepository(),
            new FakeQualificationTypeRepository(),
            new FakeEmployeeQualificationRepository(),
            new FakeCraftRoleRepository([craftRole]),
            new FakeCraftRoleQualificationRepository(),
            new FakeSeniorityRepository([]),
            new FakeRosterRepository([Roster.Create(craftCtrlNbr, ControlNumber.Create(900), null, "Trainman", "Trainmen", 1)]));

        var result = await sut.CheckEligibilityAsync(
            employeeCtrlNbr,
            positionSlotCtrlNbr,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsEligible);
        Assert.Contains(result.BlockingReasons, r => r.RuleCode == "CRAFT_MEMBERSHIP_MISSING");
    }

    [Fact]
    public async Task CheckEligibilityAsync_WhenCraftMembershipPresent_AllowsEligibility()
    {
        var employeeCtrlNbr = ControlNumber.Create(10);
        var positionSlotCtrlNbr = ControlNumber.Create(100);
        var craftCtrlNbr = ControlNumber.Create(200);
        var craftRole = CraftRole.Create(craftCtrlNbr, "TRN", "Trainman");
        var roster = Roster.Create(craftCtrlNbr, ControlNumber.Create(900), null, "Trainman", "Trainmen", 1);

        var slotRequirementRepository = new FakeSlotRequirementRepository([
            SlotRequirement.Create(positionSlotCtrlNbr, 1, craftRoleCtrlNbr: craftRole.CtrlNbr)
        ]);

        var seniority = Seniority.Create(
            roster.CtrlNbr,
            employeeCtrlNbr,
            lastActiveRoster: true,
            rosterDate: DateTime.UtcNow.AddDays(-60),
            rank: 1,
            seniorityStateCtrlNbr: ControlNumber.Create(1),
            canTrain: true);

        var sut = new EmployeeEligibilityService(
            slotRequirementRepository,
            new FakePositionSlotRepository(),
            new FakeQualificationTypeRepository(),
            new FakeEmployeeQualificationRepository(),
            new FakeCraftRoleRepository([craftRole]),
            new FakeCraftRoleQualificationRepository(),
            new FakeSeniorityRepository([seniority]),
            new FakeRosterRepository([roster]));

        var result = await sut.CheckEligibilityAsync(
            employeeCtrlNbr,
            positionSlotCtrlNbr,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsEligible);
        Assert.Empty(result.BlockingReasons);
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

    private sealed class FakeSlotRequirementRepository(IReadOnlyList<SlotRequirement> requirements)
        : FakeRepositoryBase<SlotRequirement>, ISlotRequirementRepository
    {
        public Task<List<SlotRequirement>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr)
            => Task.FromResult(requirements.Where(r => r.PositionSlotCtrlNbr == positionSlotCtrlNbr).ToList());
    }

    private sealed class FakePositionSlotRepository : FakeRepositoryBase<PositionSlot>, IPositionSlotRepository
    {
        public Task<List<PositionSlot>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr) => Task.FromResult(new List<PositionSlot>());
        public Task<List<PositionSlot>> GetOpenByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr) => Task.FromResult(new List<PositionSlot>());
    }

    private sealed class FakeCraftRoleQualificationRepository : FakeRepositoryBase<CraftRoleQualification>, ICraftRoleQualificationRepository
    {
        public Task<List<CraftRoleQualification>> GetByCraftRoleAsync(ControlNumber craftRoleCtrlNbr) => Task.FromResult(new List<CraftRoleQualification>());
    }

    private sealed class FakeQualificationTypeRepository : FakeRepositoryBase<QualificationType>, IQualificationTypeRepository
    {
        public Task<List<QualificationType>> GetByParentCtrlNbrAsync(ControlNumber parentCtrlNbr) => Task.FromResult(new List<QualificationType>());
        public Task<QualificationType?> GetByCodeAsync(ControlNumber parentCtrlNbr, string code) => Task.FromResult<QualificationType?>(null);
        public Task<List<QualificationType>> GetActiveByParentCtrlNbrAsync(ControlNumber parentCtrlNbr) => Task.FromResult(new List<QualificationType>());
    }

    private sealed class FakeEmployeeQualificationRepository : FakeRepositoryBase<EmployeeQualification>, IEmployeeQualificationRepository
    {
        public Task<List<EmployeeQualification>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr) => Task.FromResult(new List<EmployeeQualification>());
        public Task<EmployeeQualification?> GetByEmployeeAndTypeAsync(ControlNumber employeeCtrlNbr, ControlNumber qualificationTypeCtrlNbr) => Task.FromResult<EmployeeQualification?>(null);
        public Task<List<EmployeeQualification>> GetActiveByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr) => Task.FromResult(new List<EmployeeQualification>());
        public Task<List<EmployeeQualification>> GetActiveByEmployeeCtrlNbrsAsync(IEnumerable<ControlNumber> employeeCtrlNbrs) => Task.FromResult(new List<EmployeeQualification>());
        public Task<List<EmployeeQualification>> GetExpiringBeforeAsync(DateTime cutoffUtc) => Task.FromResult(new List<EmployeeQualification>());
    }

    private sealed class FakeCraftRoleRepository(IReadOnlyList<CraftRole> craftRoles)
        : FakeRepositoryBase<CraftRole>, ICraftRoleRepository
    {
        public override Task<CraftRole?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult<CraftRole?>(craftRoles.SingleOrDefault(c => c.CtrlNbr == ctrlNbr));

        public Task<List<CraftRole>> GetByCraftAsync(ControlNumber craftCtrlNbr)
            => Task.FromResult(craftRoles.Where(c => c.CraftCtrlNbr == craftCtrlNbr).ToList());

        public Task<List<CraftRole>> GetByDepartmentAsync(ControlNumber departmentCtrlNbr)
            => Task.FromResult(new List<CraftRole>());

        public Task<List<CraftRole>> GetByRailroadAsync(ControlNumber railroadCtrlNbr)
            => Task.FromResult(new List<CraftRole>());

        public Task<CraftRole?> GetByCtrlNbrWithQualificationsAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult<CraftRole?>(craftRoles.SingleOrDefault(c => c.CtrlNbr == ctrlNbr));
    }

    private sealed class FakeSeniorityRepository(IReadOnlyList<Seniority> seniorities)
        : FakeRepositoryBase<Seniority>, ISeniorityRepository
    {
        public Task<List<Seniority>> GetByRosterCtrlNbrAsync(ControlNumber rosterCtrlNbr)
            => Task.FromResult(seniorities.Where(s => s.RosterCtrlNbr == rosterCtrlNbr).ToList());

        public Task<List<Seniority>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr)
            => Task.FromResult(seniorities.Where(s => s.EmployeeCtrlNbr == employeeCtrlNbr).ToList());
    }

    private sealed class FakeRosterRepository(IReadOnlyList<Roster> rosters)
        : FakeRepositoryBase<Roster>, IRosterRepository
    {
        public Task<List<Roster>> GetByCraftCtrlNbrAsync(ControlNumber craftCtrlNbr)
            => Task.FromResult(rosters.Where(r => r.CraftCtrlNbr == craftCtrlNbr).ToList());

        public Task<List<Roster>> GetByCraftCtrlNbrsAsync(IEnumerable<ControlNumber> craftCtrlNbrs)
        {
            var set = craftCtrlNbrs.ToHashSet();
            return Task.FromResult(rosters.Where(r => set.Contains(r.CraftCtrlNbr)).ToList());
        }
    }
}
