using CrewService.Application.Authorization;

namespace CrewService.UnitTests.Fixtures;

internal sealed class TestRequestActorContextResolver : IRequestActorContextResolver
{
    public Task<RequestActorContext> ResolveAsync(
        long? requestedEmployeeCtrlNbr = null,
        long? parentCtrlNbr = null,
        long? railroadCtrlNbr = null,
        long? workAreaCtrlNbr = null,
        CancellationToken ct = default)
    {
        var context = new RequestActorContext(
            CurrentUserId: "00000000-0000-0000-0000-000000000001",
            CurrentEmployeeCtrlNbr: null,
            RequestedEmployeeCtrlNbr: requestedEmployeeCtrlNbr,
            IsLinkedEmployee: false,
            IsSelfEmployeeContext: false,
            IsActingOnBehalfOfEmployee: requestedEmployeeCtrlNbr.HasValue,
            ParentCtrlNbr: parentCtrlNbr,
            RailroadCtrlNbr: railroadCtrlNbr,
            WorkAreaCtrlNbr: workAreaCtrlNbr);

        return Task.FromResult(context);
    }
}
