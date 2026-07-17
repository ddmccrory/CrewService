using CrewService.Application.Authorization;
using Xunit;

namespace CrewService.UnitTests.Authorization;

public sealed class RequestActorContextPolicyTests
{
    private readonly RequestActorContextPolicy policy = new();

    [Fact]
    public void ShouldUseEmployeeBehavior_EmployeeRoleSelfEquivalent_ReturnsTrue()
    {
        var context = BuildContext(currentEmployeeCtrlNbr: 1001, requestedEmployeeCtrlNbr: 1001, isLinkedEmployee: true);

        Assert.True(policy.ShouldUseEmployeeBehavior(context));
    }

    [Fact]
    public void ShouldUseEmployeeBehavior_LinkedEmployeeNotSelf_ReturnsFalse()
    {
        var context = BuildContext(currentEmployeeCtrlNbr: 1001, requestedEmployeeCtrlNbr: 2002, isLinkedEmployee: true);

        Assert.False(policy.ShouldUseEmployeeBehavior(context));
    }

    [Fact]
    public void ShouldUseEmployeeBehavior_NoLinkedEmployeeSelfNotMeaningful_ReturnsFalse()
    {
        var context = BuildContext(currentEmployeeCtrlNbr: null, requestedEmployeeCtrlNbr: null, isLinkedEmployee: false);

        Assert.False(policy.ShouldUseEmployeeBehavior(context));
    }

    [Fact]
    public void CanAccessRequestedEmployee_SelfContext_AllowsWithoutOnBehalfPermission()
    {
        var context = BuildContext(currentEmployeeCtrlNbr: 1001, requestedEmployeeCtrlNbr: 1001, isLinkedEmployee: true);

        Assert.True(policy.CanAccessRequestedEmployee(context, allowOnBehalf: false));
    }

    [Fact]
    public void CanAccessRequestedEmployee_OnBehalfWithoutPermission_Denies()
    {
        var context = BuildContext(currentEmployeeCtrlNbr: 1001, requestedEmployeeCtrlNbr: 2002, isLinkedEmployee: true);

        Assert.False(policy.CanAccessRequestedEmployee(context, allowOnBehalf: false));
    }

    [Fact]
    public void CanAccessRequestedEmployee_OnBehalfWithPermission_Allows()
    {
        var context = BuildContext(currentEmployeeCtrlNbr: null, requestedEmployeeCtrlNbr: 2002, isLinkedEmployee: false);

        Assert.True(policy.CanAccessRequestedEmployee(context, allowOnBehalf: true));
    }

    private static RequestActorContext BuildContext(long? currentEmployeeCtrlNbr, long? requestedEmployeeCtrlNbr, bool isLinkedEmployee)
    {
        var isSelf = isLinkedEmployee
            && currentEmployeeCtrlNbr.HasValue
            && requestedEmployeeCtrlNbr.HasValue
            && currentEmployeeCtrlNbr.Value == requestedEmployeeCtrlNbr.Value;

        var isOnBehalf = requestedEmployeeCtrlNbr.HasValue
            && requestedEmployeeCtrlNbr != currentEmployeeCtrlNbr;

        return new RequestActorContext(
            CurrentUserId: "user-1",
            CurrentEmployeeCtrlNbr: currentEmployeeCtrlNbr,
            RequestedEmployeeCtrlNbr: requestedEmployeeCtrlNbr,
            IsLinkedEmployee: isLinkedEmployee,
            IsSelfEmployeeContext: isSelf,
            IsActingOnBehalfOfEmployee: isOnBehalf,
            ParentCtrlNbr: 10,
            RailroadCtrlNbr: 20,
            WorkAreaCtrlNbr: 30);
    }
}
