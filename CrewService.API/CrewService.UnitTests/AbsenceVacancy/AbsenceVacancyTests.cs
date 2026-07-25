using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;
using CrewService.Application.AbsenceVacancy;
using Xunit;

namespace CrewService.UnitTests.AbsenceVacancy;

public class AbsenceCodeTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var code = AbsenceCode.Create(1, "VAC", "Vacation", true, true, true, false, false, 8m, true);

        Assert.Equal("VAC", code.Code);
        Assert.True(code.IsExcused);
        Assert.True(code.IsCompensated);
        Assert.True(code.RequiresApproval);
        Assert.Equal(8m, code.DefaultAutoMarkUpHours);
    }

    [Fact]
    public void Update_ChangesOnlySpecifiedFields()
    {
        var code = AbsenceCode.Create(1, "VAC", "Vacation", true, true, true, false, false, 8m, true);

        code.Update(description: "PTO", isActive: false);

        Assert.Equal("PTO", code.Description);
        Assert.False(code.IsActive);
        Assert.True(code.IsExcused);
    }

    [Fact]
    public async Task StaticApprovalPolicyResolver_MapsRequiresApprovalFalse_ToAutomatic()
    {
        var code = AbsenceCode.Create(1, "SICK", "Sick", true, false, requiresApproval: false, false, false, null, true);
        var resolver = new StaticAbsenceApprovalPolicyResolver();

        var policy = await resolver.ResolveAsync(code, TestContext.Current.CancellationToken);

        Assert.Equal(AbsenceApprovalLevel.Automatic, policy.Level);
        Assert.Equal("Automatic approval (System)", policy.Description);
    }

    [Fact]
    public async Task StaticApprovalPolicyResolver_MapsRequiresApprovalTrue_ToCallerManager()
    {
        var code = AbsenceCode.Create(1, "VAC", "Vacation", true, true, requiresApproval: true, false, false, 8m, true);
        var resolver = new StaticAbsenceApprovalPolicyResolver();

        var policy = await resolver.ResolveAsync(code, TestContext.Current.CancellationToken);

        Assert.Equal(AbsenceApprovalLevel.CallerManager, policy.Level);
        Assert.Equal("Caller or Manager approval required", policy.Description);
    }

    [Fact]
    public async Task DbApprovalPolicyResolver_UsesRepositoryDirectly_AndDoesNotRequireUow()
    {
        var code = AbsenceCode.Create(1, "VAC", "Vacation", true, true, requiresApproval: true, false, false, 8m, true);
        var repository = new FakeAbsenceApprovalPolicyRepository(
            policy: Domain.Modules.Policies.AbsenceApprovalPolicy.Create(
                railroadCtrlNbr: ControlNumber.Create(1),
                approvalLevel: AbsenceApprovalPolicyLevel.ManagerOnly,
                isEnabled: true,
                autoMarkOffIfWithinHoursEnabled: true,
                autoMarkOffIfWithinHours: 6));
        var resolver = new DbAbsenceApprovalPolicyResolver(repository);

        var policy = await resolver.ResolveAsync(code, TestContext.Current.CancellationToken);

        Assert.Equal(AbsenceApprovalLevel.ManagerOnly, policy.Level);
        Assert.Equal("Manager approval required", policy.Description);
        Assert.True(policy.AutoMarkOffIfWithinHoursEnabled);
        Assert.Equal(6, policy.AutoMarkOffIfWithinHours);
        Assert.Equal(1, repository.GetByRailroadCallCount);
    }

    [Fact]
    public void AbsenceApprovalPolicy_Create_WithAutoMarkOffThreshold_SetsProperties()
    {
        var policy = Domain.Modules.Policies.AbsenceApprovalPolicy.Create(
            railroadCtrlNbr: ControlNumber.Create(7),
            approvalLevel: AbsenceApprovalPolicyLevel.CallerManager,
            isEnabled: true,
            autoMarkOffIfWithinHoursEnabled: true,
            autoMarkOffIfWithinHours: 4);

        Assert.True(policy.AutoMarkOffIfWithinHoursEnabled);
        Assert.Equal(4, policy.AutoMarkOffIfWithinHours);
    }

    private sealed class FakeAbsenceApprovalPolicyRepository(Domain.Modules.Policies.AbsenceApprovalPolicy? policy)
        : IAbsenceApprovalPolicyRepository
    {
        public int GetByRailroadCallCount { get; private set; }

        public Task<Domain.Modules.Policies.AbsenceApprovalPolicy?> GetByRailroadAsync(ControlNumber railroadCtrlNbr)
        {
            GetByRailroadCallCount++;
            return Task.FromResult(policy);
        }

        public Task<List<Domain.Modules.Policies.AbsenceApprovalPolicy>> GetAllAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<Domain.Modules.Policies.AbsenceApprovalPolicy>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Domain.Modules.Policies.AbsenceApprovalPolicy?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Domain.Modules.Policies.AbsenceApprovalPolicy?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(Domain.Modules.Policies.AbsenceApprovalPolicy entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(Domain.Modules.Policies.AbsenceApprovalPolicy entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public void Add(Domain.Modules.Policies.AbsenceApprovalPolicy entity) => throw new NotImplementedException();
        public void Update(Domain.Modules.Policies.AbsenceApprovalPolicy entity) => throw new NotImplementedException();
        public void Remove(Domain.Modules.Policies.AbsenceApprovalPolicy entity) => throw new NotImplementedException();
    }
}

public class AbsenceRequestTests
{
    [Fact]
    public void Create_DefaultsToPending()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        Assert.Equal("PENDING", request.DerivedStatus);
        Assert.Equal("VAC", request.ReasonCode);
        Assert.Null(request.ApprovedByCtrlNbr);
        Assert.True(request.DomainEvents.Count > 0);
    }

    [Fact]
    public void Approve_SetsApprovedStatus()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        request.Approve(200);

        Assert.Equal("APPROVED", request.DerivedStatus);
        Assert.Equal(200, request.ApprovedByCtrlNbr!.Value);
    }

    [Fact]
    public void Deny_SetsDeniedStatus()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        request.Deny(200);

        Assert.Equal("DENIED", request.DerivedStatus);
    }

    [Fact]
    public void Cancel_SetsCancelledStatus()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        request.Cancel();

        Assert.Equal("CANCELLED", request.DerivedStatus);
    }

    [Fact]
    public void CompleteByMarkUp_LeavesStatusExercised()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");
        request.Approve(200);
        request.Exercise(DateTime.UtcNow);
        var markUpTime = DateTime.UtcNow.AddHours(8);

        request.CompleteByMarkUp(markUpTime);

        Assert.Equal("COMPLETE", request.DerivedStatus);
        Assert.Single(request.EndRecords);
    }

    [Fact]
    public void Approve_DoesNotAutoCompleteRequest()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        request.Approve(200);

        Assert.Equal("APPROVED", request.DerivedStatus);
        Assert.Empty(request.EndRecords);
    }

    [Fact]
    public void CompleteByMarkUp_TransitionsApprovedRequestToExercisedWhenNeeded()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");
        request.Approve(200);
        request.Exercise(DateTime.UtcNow);
        var completionUtc = DateTime.UtcNow.AddHours(4);

        request.CompleteByMarkUp(completionUtc);

        Assert.Equal("COMPLETE", request.DerivedStatus);
        Assert.Single(request.EndRecords);
    }

    [Fact]
    public void Approve_SetsApprovedMetadata()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        request.Approve(ControlNumber.Create(200));

        Assert.Equal(200, request.ApprovedByCtrlNbr!.Value);
        Assert.NotNull(request.ApprovedAtUtc);
    }

    [Fact]
    public void AddEndRecord_AddsToCollection()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");
        request.Approve(ControlNumber.Create(200));
        var startTime = DateTime.UtcNow;
        request.AddStartRecord(startTime);
        var endTime = startTime.AddHours(8);

        var endRecord = request.AddEndRecord(endTime, true);

        Assert.Single(request.EndRecords);
        Assert.True(endRecord.IsAutoEndRecord);
    }

    [Fact]
    public void Exercise_ApprovedRequestWithNoEnd_SetsExercisedAndLeavesNoMarkUps()
    {
        var exercisedUtc = DateTime.UtcNow;
        var request = AbsenceRequest.Create(100, exercisedUtc.AddHours(-1), null, "VAC");
        request.Approve(200);

        request.Exercise(exercisedUtc);

        Assert.Equal("OPEN", request.DerivedStatus);
        Assert.Equal(DateTime.SpecifyKind(exercisedUtc.AddHours(-1), DateTimeKind.Utc), request.ScheduledStartUtc);
        Assert.Single(request.StartRecords);
        Assert.Equal(DateTime.SpecifyKind(exercisedUtc, DateTimeKind.Utc), request.StartRecords[0].ActualStartUtc);
        Assert.Empty(request.EndRecords);
    }

    [Fact]
    public void Exercise_ClampsMarkOffStartToScheduledStartWhenExercisedEarlier()
    {
        var scheduledStartUtc = DateTime.UtcNow.AddHours(3);
        var exercisedUtc = DateTime.UtcNow;
        var request = AbsenceRequest.Create(100, scheduledStartUtc, null, "VAC");
        request.Approve(200);

        request.Exercise(exercisedUtc);

        Assert.Equal(DateTime.SpecifyKind(scheduledStartUtc, DateTimeKind.Utc), request.ScheduledStartUtc);
        Assert.Single(request.StartRecords);
        Assert.Equal(DateTime.SpecifyKind(scheduledStartUtc, DateTimeKind.Utc), request.StartRecords[0].ActualStartUtc);
    }

    [Fact]
    public void Exercise_ApprovedRequestWithScheduledEnd_DoesNotAutoCreateEndRecord()
    {
        var exercisedUtc = DateTime.UtcNow;
        var scheduledEndUtc = exercisedUtc.AddHours(8);
        var request = AbsenceRequest.Create(100, exercisedUtc.AddHours(-1), scheduledEndUtc, "VAC");
        request.Approve(200);

        request.Exercise(exercisedUtc);

        Assert.Equal("OPEN", request.DerivedStatus);
        Assert.Equal(DateTime.SpecifyKind(scheduledEndUtc, DateTimeKind.Utc), request.ScheduledEndUtc);
        Assert.Empty(request.EndRecords);
    }

    [Fact]
    public void Exercise_NonApprovedRequest_ThrowsInvalidOperationException()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        Assert.Throws<InvalidOperationException>(() => request.Exercise(DateTime.UtcNow));
    }

    [Fact]
    public void CreateWithCode_SetsAutoMarkOffOnApprovalFlag()
    {
        var request = AbsenceRequest.CreateWithCode(
            employeeCtrlNbr: ControlNumber.Create(100),
            startUtc: DateTime.UtcNow,
            endUtc: null,
            absenceCodeCtrlNbr: ControlNumber.Create(10),
            reasonCode: "MARKOFF",
            isSystemGenerated: false,
            notes: null,
            autoMarkOffOnApproval: true);

        Assert.True(request.AutoMarkOffOnApproval);
    }
}

public class AbsenceCodeCraftOverrideTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var ov = AbsenceCodeCraftOverride.Create(
            ControlNumber.Create(1), ControlNumber.Create(10), 12m);

        Assert.Equal(1, ov.AbsenceCodeCtrlNbr.Value);
        Assert.Equal(10, ov.CraftCtrlNbr.Value);
        Assert.Equal(12m, ov.OverrideAutoMarkUpHours);
    }
}

public class CompensationBalanceTests
{
    [Fact]
    public void Create_SetsInitialBalance()
    {
        var balance = CompensationBalance.Create(
            ControlNumber.Create(100), "VACATION", 80m);

        Assert.Equal("VACATION", balance.CompensationType);
        Assert.Equal(80m, balance.BalanceHours);
    }

    [Fact]
    public void Debit_SufficientBalance_ReturnsTrue()
    {
        var balance = CompensationBalance.Create(
            ControlNumber.Create(100), "VACATION", 80m);

        var result = balance.Debit(8m);

        Assert.True(result);
        Assert.Equal(72m, balance.BalanceHours);
    }

    [Fact]
    public void Debit_InsufficientBalance_ReturnsFalse()
    {
        var balance = CompensationBalance.Create(
            ControlNumber.Create(100), "VACATION", 4m);

        var result = balance.Debit(8m);

        Assert.False(result);
        Assert.Equal(4m, balance.BalanceHours);
    }

    [Fact]
    public void Credit_IncreasesBalance()
    {
        var balance = CompensationBalance.Create(
            ControlNumber.Create(100), "VACATION", 80m);

        balance.Credit(16m);

        Assert.Equal(96m, balance.BalanceHours);
    }
}
