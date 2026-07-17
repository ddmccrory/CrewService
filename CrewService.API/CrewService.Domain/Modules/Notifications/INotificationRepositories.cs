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

    /// <summary>
    /// Returns every notification for a railroad (newest first), including acknowledgement
    /// attempts. Drives the read-only railroad-wide reference menu.
    /// </summary>
    Task<List<EmployeeNotification>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default);

    /// <summary>
    /// Counts the railroad's open notices (require acknowledgement, not yet confirmed) for the
    /// reference-menu badge.
    /// </summary>
    Task<int> CountUnacknowledgedByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default);

    /// <summary>
    /// Returns open position-change artifacts linked to a specific notification.
    /// Used to close parity projection records when acknowledgements/cancel flows complete.
    /// </summary>
    Task<List<PositionChangeRecord>> GetOpenPositionChangesByNotificationAsync(
        ControlNumber employeeNotificationCtrlNbr,
        CancellationToken ct = default)
        => Task.FromResult(new List<PositionChangeRecord>());
}

public interface INotificationTypeConfigRepository : IRepository<NotificationTypeConfig>
{
    Task<List<NotificationTypeConfig>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default);
    Task<NotificationTypeConfig?> GetByRailroadAndKeyAsync(ControlNumber railroadCtrlNbr, string key, CancellationToken ct = default);
}

public interface IPositionChangeRecordRepository : IRepository<PositionChangeRecord>
{
    Task<List<PositionChangeRecord>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);
    Task<List<PositionChangeRecord>> GetOpenByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);
    Task<List<PositionChangeRecord>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default);
    Task<List<PositionChangeRecord>> GetOpenByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default);
    Task<List<PositionChangeRecord>> GetOpenBySourceAsync(string sourceType, ControlNumber sourceCtrlNbr, CancellationToken ct = default);
}
