using CrewService.Domain.Interfaces;

namespace CrewService.UnitTests.Fixtures;

internal sealed class TestCurrentUserService : ICurrentUserService
{
    public Guid GetUserId() => Guid.Parse("00000000-0000-0000-0000-000000000001");
    public string GetUserName() => "test-user";
    public long? GetParentCtrlNbr() => null;
    public void SetAuditOverride(string name) { }
}
