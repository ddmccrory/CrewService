using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Policies;

public class CraftOperationsPolicyTests
{
    [Fact]
    public void Create_UsesDefaults()
    {
        var policy = CraftOperationsPolicy.Create(ControlNumber.Create(10));

        Assert.Equal(90, policy.LateCallThresholdMinutes);
        Assert.Equal("FRA", policy.RestCalculationStrategy);
        Assert.Null(policy.FixedRestHours);
        Assert.Equal(24m, policy.ConsecutiveDayResetHours);
        Assert.False(policy.DeleteConflictingNextShift);
        Assert.False(policy.AutoAnnulCreatesOffDuty);
    }

    [Fact]
    public void Update_ChangesOnlySpecifiedFields()
    {
        var policy = CraftOperationsPolicy.Create(ControlNumber.Create(10));

        policy.Update(lateCallThresholdMinutes: 60, autoAnnulCreatesOffDuty: true);

        Assert.Equal(60, policy.LateCallThresholdMinutes);
        Assert.True(policy.AutoAnnulCreatesOffDuty);
        Assert.Equal("FRA", policy.RestCalculationStrategy);
    }
}
