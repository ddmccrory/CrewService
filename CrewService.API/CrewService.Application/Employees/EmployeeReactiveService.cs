using CrewService.Application.Workflows;
using CrewService.Domain.DomainEvents;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.Employees;

public sealed class EmployeeReactiveService(
    WorkflowRuntimeService workflowRuntimeService,
    ILogger<EmployeeReactiveService> logger)
{
    public async Task HandleEmployeeCreatedAsync(DomainEvent domainEvent, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(domainEvent.PayloadJson))
        {
            logger.LogWarning("EmployeeReactiveService: EmployeeCreatedDomainEvent has no payload. EventId: {EventId}", domainEvent.EventId);
            return;
        }

        try
        {
            await workflowRuntimeService.ExecuteEmployeeCreatedAsync(domainEvent, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EmployeeReactiveService: Employee-created workflow execution failed. EventId: {EventId}", domainEvent.EventId);
            throw;
        }
    }
}
