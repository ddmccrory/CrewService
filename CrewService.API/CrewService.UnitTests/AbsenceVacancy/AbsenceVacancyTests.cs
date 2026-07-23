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
                isEnabled: true));
        var resolver = new DbAbsenceApprovalPolicyResolver(repository);

        var policy = await resolver.ResolveAsync(code, TestContext.Current.CancellationToken);

        Assert.Equal(AbsenceApprovalLevel.ManagerOnly, policy.Level);
        Assert.Equal("Manager approval required", policy.Description);
        Assert.Equal(1, repository.GetByRailroadCallCount);
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

        Assert.Equal("PENDING", request.Status);
        Assert.Equal("VAC", request.ReasonCode);
        Assert.Null(request.ApprovedByCtrlNbr);
        Assert.True(request.DomainEvents.Count > 0);
    }

    [Fact]
    public void Approve_SetsApprovedStatus()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        request.Approve(200);

        Assert.Equal("APPROVED", request.Status);
        Assert.Equal(200, request.ApprovedByCtrlNbr!.Value);
    }

    [Fact]
    public void Deny_SetsDeniedStatus()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        request.Deny(200);

        Assert.Equal("DENIED", request.Status);
    }

    [Fact]
    public void Cancel_SetsCancelledStatus()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        request.Cancel();

        Assert.Equal("CANCELLED", request.Status);
    }

    [Fact]
    public void CompleteByMarkUp_LeavesStatusExercised()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");
        request.Approve(200);
        request.Exercise(DateTime.UtcNow);
        var markUpTime = DateTime.UtcNow.AddHours(8);

        request.CompleteByMarkUp(markUpTime);

        Assert.Equal("EXERCISED", request.Status);
    }

    [Fact]
    public void Approve_DoesNotAutoCompleteRequest()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        request.Approve(200);

        Assert.Equal("APPROVED", request.Status);
        Assert.Empty(request.MarkUps);
    }

    [Fact]
    public void CompleteByMarkUp_TransitionsApprovedRequestToExercisedWhenNeeded()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");
        request.Approve(200);
        var completionUtc = DateTime.UtcNow.AddHours(4);

        request.CompleteByMarkUp(completionUtc);

        Assert.Equal("EXERCISED", request.Status);
    }

    [Fact]
    public void AddApproval_AddsToCollection()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        var approval = request.AddApproval(ControlNumber.Create(200));

        Assert.Single(request.Approvals);
        Assert.Equal(200, approval.ApprovalOfficerCtrlNbr.Value);
    }

    [Fact]
    public void AddMarkUp_AddsToCollection()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");
        var scheduledTime = DateTime.UtcNow.AddHours(8);

        var markUp = request.AddMarkUp(scheduledTime, true);

        Assert.Single(request.MarkUps);
        Assert.True(markUp.IsAutoMarkUp);
    }

    [Fact]
    public void Exercise_ApprovedRequestWithNoEnd_SetsExercisedAndLeavesNoMarkUps()
    {
        var exercisedUtc = DateTime.UtcNow;
        var request = AbsenceRequest.Create(100, exercisedUtc.AddHours(-1), null, "VAC");
        request.Approve(200);

        request.Exercise(exercisedUtc);

        Assert.Equal("EXERCISED", request.Status);
        Assert.Equal(DateTime.SpecifyKind(exercisedUtc.AddHours(-1), DateTimeKind.Utc), request.ScheduledStartUtc);
        Assert.Equal(DateTime.SpecifyKind(exercisedUtc, DateTimeKind.Utc), request.MarkOffStartUtc);
        Assert.Empty(request.MarkUps);
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
        Assert.Equal(DateTime.SpecifyKind(scheduledStartUtc, DateTimeKind.Utc), request.MarkOffStartUtc);
    }

    [Fact]
    public void Exercise_ApprovedRequestWithScheduledEnd_CreatesAutoMarkUp()
    {
        var exercisedUtc = DateTime.UtcNow;
        var scheduledEndUtc = exercisedUtc.AddHours(8);
        var request = AbsenceRequest.Create(100, exercisedUtc.AddHours(-1), scheduledEndUtc, "VAC");
        request.Approve(200);

        request.Exercise(exercisedUtc);

        Assert.Equal("EXERCISED", request.Status);
        Assert.Single(request.MarkUps);
        var markUp = request.MarkUps[0];
        Assert.Equal(DateTime.SpecifyKind(scheduledEndUtc, DateTimeKind.Utc), markUp.ScheduledMarkUpUtc);
        Assert.True(markUp.IsAutoMarkUp);
        Assert.Null(markUp.ActualMarkUpUtc);
    }

    [Fact]
    public void Exercise_NonApprovedRequest_ThrowsInvalidOperationException()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        Assert.Throws<InvalidOperationException>(() => request.Exercise(DateTime.UtcNow));
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
