using CrewService.Application.Payroll;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Payroll;

public class EarningCodeResolverTests
{
    private sealed class FakeRuleRepo : IEarningCodeRuleRepository
    {
        private readonly List<EarningCodeRule> _rules;
        public FakeRuleRepo(List<EarningCodeRule> rules) => _rules = rules;
        public Task<IReadOnlyList<EarningCodeRule>> GetActiveByWorkAreaAsync(
            ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EarningCodeRule>>(_rules);
    }

    [Fact]
    public async Task Resolve_OffDayNotHoliday_ReturnsOT()
    {
        var rules = new List<EarningCodeRule>
        {
            EarningCodeRule.Create(ControlNumber.Create(1), 1, "IsOffDay=true,IsHoliday=false", "OT", false, true),
            EarningCodeRule.Create(ControlNumber.Create(1), 2, "IsOffDay=true,IsHoliday=true", "HO", false, true),
        };

        var resolver = new EarningCodeResolver(new FakeRuleRepo(rules));
        var result = await resolver.ResolveAsync(ControlNumber.Create(1),
            new EarningContext(true, false, false, null, null));

        Assert.NotNull(result);
        Assert.Equal("OT", result!.ResultCode);
    }

    [Fact]
    public async Task Resolve_OffDayHoliday_ReturnsHO()
    {
        var rules = new List<EarningCodeRule>
        {
            EarningCodeRule.Create(ControlNumber.Create(1), 1, "IsOffDay=true,IsHoliday=false", "OT", false, true),
            EarningCodeRule.Create(ControlNumber.Create(1), 2, "IsOffDay=true,IsHoliday=true", "HO", false, true),
        };

        var resolver = new EarningCodeResolver(new FakeRuleRepo(rules));
        var result = await resolver.ResolveAsync(ControlNumber.Create(1),
            new EarningContext(true, true, false, null, null));

        Assert.NotNull(result);
        Assert.Equal("HO", result!.ResultCode);
    }

    [Fact]
    public async Task Resolve_NoMatch_ReturnsNull()
    {
        var rules = new List<EarningCodeRule>
        {
            EarningCodeRule.Create(ControlNumber.Create(1), 1, "IsOffDay=true", "OT", false, true),
        };

        var resolver = new EarningCodeResolver(new FakeRuleRepo(rules));
        var result = await resolver.ResolveAsync(ControlNumber.Create(1),
            new EarningContext(false, false, false, null, null));

        Assert.Null(result);
    }
}

public class PayRateTests
{
    [Fact]
    public void CalculatePay_Regular_ReturnsBaseRate()
    {
        var rate = PayRate.Create(ControlNumber.Create(1), DateTime.UtcNow, 25m);
        Assert.Equal(200m, rate.CalculatePay(8m, false));
    }

    [Fact]
    public void CalculatePay_Overtime_AppliesMultiplier()
    {
        var rate = PayRate.Create(ControlNumber.Create(1), DateTime.UtcNow, 25m, 1.5m);
        Assert.Equal(75m, rate.CalculatePay(2m, true));
    }
}

public class EarningApprovalTests
{
    [Fact]
    public void Approve_SetsStatusAndTimestamp()
    {
        var approval = EarningApproval.Create(
            ControlNumber.Create(1), 1, ControlNumber.Create(99));
        approval.Approve();

        Assert.Equal("APPROVED", approval.Status);
        Assert.NotNull(approval.DecidedAtUtc);
    }

    [Fact]
    public void Decline_SetsStatusAndTimestamp()
    {
        var approval = EarningApproval.Create(
            ControlNumber.Create(1), 1, ControlNumber.Create(99));
        approval.Decline();

        Assert.Equal("DECLINED", approval.Status);
        Assert.NotNull(approval.DecidedAtUtc);
    }
}

public class PayrollRecordTests
{
    [Fact]
    public void SetEarningCode_SetsProperties()
    {
        var record = PayrollRecord.Create(1, 2, "REG", 200m, 8m);
        record.SetEarningCode("OT", true, ControlNumber.Create(100));

        Assert.Equal("OT", record.ResolvedEarningCode);
        Assert.True(record.RequiresApproval);
        Assert.Equal(100, record.OnDutyRecordCtrlNbr!.Value);
    }
}
