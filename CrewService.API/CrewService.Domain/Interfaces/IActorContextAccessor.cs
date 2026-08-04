namespace CrewService.Domain.Interfaces;

public interface IActorContextAccessor
{
    ActorContext? Current { get; }

    IDisposable BeginScope(ActorContext context);
}
