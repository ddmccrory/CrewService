using CrewService.Application.Qualifications;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Qualifications;

public sealed class RequirementEvaluationServiceTests
{
    [Fact]
    public async Task EvaluateAsync_WhenAllPrerequisitesSatisfied_CreatesPendingQualificationWithEvidence()
    {
        var employeeCtrlNbr = ControlNumber.Create(10);
        var parentCtrlNbr = ControlNumber.Create(20);

        var qualificationType = QualificationType.Create(
            parentCtrlNbr,
            "FOREMAN",
            "Foreman",
            evaluationStrategy: EvaluationStrategies.ActivityCount,
            expirationMonths: 12,
            isBlocking: true);

        var Requirement = qualificationType.AddRequirement(
            requirementKind: RequirementKinds.ActivityCount,
            threshold: 90,
            thresholdUnit: ThresholdUnits.Count,
            description: "90 trips required");

        var prerequisiteRepository = new FakeQualificationRequirementRepository([Requirement]);
        var qualificationRepository = new FakeEmployeeQualificationRepository();

        var sut = new RequirementEvaluationService(
            [new AlwaysSatisfiedEvaluator(RequirementKinds.ActivityCount, "90 qualifying on-duty records")],
            prerequisiteRepository,
            qualificationRepository);

        var result = await sut.EvaluateAsync(
            employeeCtrlNbr,
            qualificationType,
            TestContext.Current.CancellationToken);

        Assert.True(result.AllSatisfied);
        Assert.True(result.QualificationCreated);

        var created = Assert.Single(qualificationRepository.AddedQualifications);
        Assert.Equal(employeeCtrlNbr, created.EmployeeCtrlNbr);
        Assert.Equal(qualificationType.CtrlNbr, created.QualificationTypeCtrlNbr);
        Assert.Equal("Pending", created.Status);
        Assert.Single(created.Evidence);
    }

    private sealed class AlwaysSatisfiedEvaluator(string kind, string description) : IRequirementEvaluator
    {
        public string Kind { get; } = kind;

        public Task<EvaluationResult> EvaluateAsync(ControlNumber employeeCtrlNbr, QualificationRequirement rule, CancellationToken ct = default)
            => Task.FromResult(EvaluationResult.Satisfied(description));
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

    private sealed class FakeQualificationRequirementRepository(IReadOnlyList<QualificationRequirement> prerequisites)
        : FakeRepositoryBase<QualificationRequirement>, IQualificationRequirementRepository
    {
        public Task<List<QualificationRequirement>> GetByQualificationTypeCtrlNbrAsync(ControlNumber qualificationTypeCtrlNbr)
            => Task.FromResult(prerequisites.Where(p => p.QualificationTypeCtrlNbr == qualificationTypeCtrlNbr).ToList());
    }

    private sealed class FakeEmployeeQualificationRepository
        : FakeRepositoryBase<EmployeeQualification>, IEmployeeQualificationRepository
    {
        public List<EmployeeQualification> AddedQualifications { get; } = [];

        public override Task AddAsync(EmployeeQualification entity, CancellationToken ct = default)
        {
            AddedQualifications.Add(entity);
            return Task.CompletedTask;
        }

        public Task<List<EmployeeQualification>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr)
            => Task.FromResult(new List<EmployeeQualification>());

        public Task<EmployeeQualification?> GetByEmployeeAndTypeAsync(ControlNumber employeeCtrlNbr, ControlNumber qualificationTypeCtrlNbr)
            => Task.FromResult<EmployeeQualification?>(null);

        public Task<List<EmployeeQualification>> GetActiveByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr)
            => Task.FromResult(new List<EmployeeQualification>());

        public Task<List<EmployeeQualification>> GetExpiringBeforeAsync(DateTime cutoffUtc)
            => Task.FromResult(new List<EmployeeQualification>());
    }
}
