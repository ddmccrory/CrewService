using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Notifications;

public interface IEmployeeNotificationRepository : IRepository<EmployeeNotification>
{
    /// <summary>
    /// Returns the employee's notifications (newest first), including acknowledgement attempts.
    /// </summary>
    Task<List<EmployeeNotification>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);

    /// <summary>
    /// Returns the employee's notifications that still require acknowledgement and have not yet
    /// been confirmed. Drives the legacy login-acknowledgement surface.
    /// </summary>
    Task<List<EmployeeNotification>> GetUnacknowledgedByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);
}
