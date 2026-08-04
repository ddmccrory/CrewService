using System.Threading;
using CrewService.Domain.Interfaces;

namespace CrewService.Persistance.Services;

public sealed class ActorContextAccessor : IActorContextAccessor
{
    private static readonly AsyncLocal<ActorContext?> CurrentActorContext = new();

    public ActorContext? Current => CurrentActorContext.Value;

    public IDisposable BeginScope(ActorContext context)
    {
        var prior = CurrentActorContext.Value;
        CurrentActorContext.Value = context;
        return new RevertScope(prior);
    }

    private sealed class RevertScope(ActorContext? prior) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            CurrentActorContext.Value = prior;
        }
    }
}
