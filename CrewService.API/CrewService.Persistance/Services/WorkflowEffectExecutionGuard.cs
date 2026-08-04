using System.Threading;
using CrewService.Domain.Interfaces;

namespace CrewService.Persistance.Services;

public sealed class WorkflowEffectExecutionGuard : IWorkflowEffectExecutionGuard
{
    private static readonly AsyncLocal<int> ScopeDepth = new();

    public bool IsInWorkflowDbEffectExecution => ScopeDepth.Value > 0;

    public IDisposable BeginWorkflowDbEffectExecutionScope()
    {
        ScopeDepth.Value = ScopeDepth.Value + 1;
        return new ScopeHandle();
    }

    private sealed class ScopeHandle : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ScopeDepth.Value = Math.Max(0, ScopeDepth.Value - 1);
        }
    }
}
