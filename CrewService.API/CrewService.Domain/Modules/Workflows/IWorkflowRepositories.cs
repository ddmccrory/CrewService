using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Workflows;

public interface IWorkflowTemplateRepository : IRepository<WorkflowTemplate>
{
    Task<List<WorkflowTemplate>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default);

    Task<List<WorkflowTemplate>> GetByRailroadAndTriggerTypeAsync(ControlNumber railroadCtrlNbr, ControlNumber triggerTypeCtrlNbr, CancellationToken ct = default);
}

public interface IWorkflowVersionRepository : IRepository<WorkflowVersion>
{
    Task<List<WorkflowVersion>> GetByTemplateAsync(ControlNumber workflowTemplateCtrlNbr, CancellationToken ct = default);

    Task<WorkflowVersion?> GetLatestPublishedByRailroadAndTriggerAsync(
        ControlNumber railroadCtrlNbr,
        ControlNumber triggerTypeCtrlNbr,
        CancellationToken ct = default);
}

public interface IWorkflowExecutionHistoryRepository : IRepository<WorkflowExecutionHistory>
{
    Task<List<WorkflowExecutionHistory>> GetByTemplateAsync(
        ControlNumber workflowTemplateCtrlNbr,
        int take = 100,
        CancellationToken ct = default);
}

public interface IWorkflowTriggerTypeRepository : IRepository<WorkflowTriggerType>
{
    Task<List<WorkflowTriggerType>> GetAllActiveAsync(CancellationToken ct = default);
    Task<WorkflowTriggerType?> GetByCodeAsync(string code, CancellationToken ct = default);
}

public interface IWorkflowEffectTypeRepository : IRepository<WorkflowEffectType>
{
    Task<List<WorkflowEffectType>> GetAllActiveAsync(CancellationToken ct = default);
    Task<WorkflowEffectType?> GetByCodeAsync(string code, CancellationToken ct = default);
}

public interface IWorkflowOperatorTypeRepository : IRepository<WorkflowOperatorType>
{
    Task<List<WorkflowOperatorType>> GetAllActiveAsync(CancellationToken ct = default);
    Task<WorkflowOperatorType?> GetByCodeAsync(string code, CancellationToken ct = default);
}

public interface IWorkflowMetadataFieldTypeRepository : IRepository<WorkflowMetadataFieldType>
{
    Task<List<WorkflowMetadataFieldType>> GetAllActiveAsync(CancellationToken ct = default);
    Task<WorkflowMetadataFieldType?> GetByCodeAsync(string code, CancellationToken ct = default);
}
