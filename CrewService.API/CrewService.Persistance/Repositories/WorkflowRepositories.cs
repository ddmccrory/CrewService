using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class WorkflowTemplateRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<WorkflowTemplate>(dbContext, currentUserService), IWorkflowTemplateRepository
{
    public async Task<List<WorkflowTemplate>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<WorkflowTemplate>()
            .Where(w => w.RailroadCtrlNbr == railroadCtrlNbr)
            .OrderBy(w => w.Name)
            .ToListAsync(ct);
    }

    public async Task<List<WorkflowTemplate>> GetByRailroadAndTriggerTypeAsync(ControlNumber railroadCtrlNbr, ControlNumber triggerTypeCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<WorkflowTemplate>()
            .Where(w => w.RailroadCtrlNbr == railroadCtrlNbr && w.TriggerTypeCtrlNbr == triggerTypeCtrlNbr)
            .OrderBy(w => w.Name)
            .ToListAsync(ct);
    }
}

internal sealed class WorkflowVersionRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<WorkflowVersion>(dbContext, currentUserService), IWorkflowVersionRepository
{
    public async Task<List<WorkflowVersion>> GetByTemplateAsync(ControlNumber workflowTemplateCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<WorkflowVersion>()
            .Where(v => v.WorkflowTemplateCtrlNbr == workflowTemplateCtrlNbr)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);
    }

    public async Task<WorkflowVersion?> GetLatestPublishedByRailroadAndTriggerAsync(
        ControlNumber railroadCtrlNbr,
        ControlNumber triggerTypeCtrlNbr,
        CancellationToken ct = default)
    {
        return await (
            from version in DbContext.Set<WorkflowVersion>()
            join template in DbContext.Set<WorkflowTemplate>() on version.WorkflowTemplateCtrlNbr equals template.CtrlNbr
            where template.RailroadCtrlNbr == railroadCtrlNbr
                  && template.TriggerTypeCtrlNbr == triggerTypeCtrlNbr
                  && template.IsEnabled
                  && version.Status == WorkflowVersionStatus.Published
            orderby version.VersionNumber descending
            select version)
            .FirstOrDefaultAsync(ct);
    }
}

internal sealed class WorkflowExecutionHistoryRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<WorkflowExecutionHistory>(dbContext, currentUserService), IWorkflowExecutionHistoryRepository
{
    public async Task<List<WorkflowExecutionHistory>> GetByTemplateAsync(
        ControlNumber workflowTemplateCtrlNbr,
        int take = 100,
        CancellationToken ct = default)
    {
        var normalizedTake = take <= 0 ? 100 : Math.Min(take, 500);

        return await DbContext.Set<WorkflowExecutionHistory>()
            .Where(h => h.WorkflowTemplateCtrlNbr == workflowTemplateCtrlNbr)
            .OrderByDescending(h => h.StartedAtUtc)
            .Take(normalizedTake)
            .ToListAsync(ct);
    }
}

internal sealed class WorkflowTriggerTypeRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<WorkflowTriggerType>(dbContext, currentUserService), IWorkflowTriggerTypeRepository
{
    public async Task<List<WorkflowTriggerType>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await DbContext.Set<WorkflowTriggerType>()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<WorkflowTriggerType?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalizedCode = code.Trim();
        return await DbContext.Set<WorkflowTriggerType>()
            .FirstOrDefaultAsync(t => t.Code == normalizedCode, ct);
    }
}

internal sealed class WorkflowEffectTypeRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<WorkflowEffectType>(dbContext, currentUserService), IWorkflowEffectTypeRepository
{
    public async Task<List<WorkflowEffectType>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await DbContext.Set<WorkflowEffectType>()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<WorkflowEffectType?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalizedCode = code.Trim();
        return await DbContext.Set<WorkflowEffectType>()
            .FirstOrDefaultAsync(t => t.Code == normalizedCode, ct);
    }
}

internal sealed class WorkflowOperatorTypeRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<WorkflowOperatorType>(dbContext, currentUserService), IWorkflowOperatorTypeRepository
{
    public async Task<List<WorkflowOperatorType>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await DbContext.Set<WorkflowOperatorType>()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<WorkflowOperatorType?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalizedCode = code.Trim();
        return await DbContext.Set<WorkflowOperatorType>()
            .FirstOrDefaultAsync(t => t.Code == normalizedCode, ct);
    }
}

internal sealed class WorkflowMetadataFieldTypeRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<WorkflowMetadataFieldType>(dbContext, currentUserService), IWorkflowMetadataFieldTypeRepository
{
    public async Task<List<WorkflowMetadataFieldType>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await DbContext.Set<WorkflowMetadataFieldType>()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<WorkflowMetadataFieldType?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalizedCode = code.Trim();
        return await DbContext.Set<WorkflowMetadataFieldType>()
            .FirstOrDefaultAsync(t => t.Code == normalizedCode, ct);
    }
}
