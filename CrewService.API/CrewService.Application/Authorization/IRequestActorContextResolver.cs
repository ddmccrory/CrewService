namespace CrewService.Application.Authorization;

public interface IRequestActorContextResolver
{
    Task<RequestActorContext> ResolveAsync(
        long? requestedEmployeeCtrlNbr = null,
        long? parentCtrlNbr = null,
        long? railroadCtrlNbr = null,
        long? workAreaCtrlNbr = null,
        CancellationToken ct = default);
}
