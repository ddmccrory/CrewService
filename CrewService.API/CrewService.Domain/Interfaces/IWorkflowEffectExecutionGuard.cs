namespace CrewService.Domain.Interfaces;

public interface IWorkflowEffectExecutionGuard
{
    bool IsInWorkflowDbEffectExecution { get; }

    IDisposable BeginWorkflowDbEffectExecutionScope();
}
