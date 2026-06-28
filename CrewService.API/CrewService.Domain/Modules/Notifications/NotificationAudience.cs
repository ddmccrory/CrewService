namespace CrewService.Domain.Modules.Notifications;

/// <summary>
/// Who a notification is intended for. Replaces the legacy EmployeeOnly boolean
/// with an explicit, orthogonal concept.
/// </summary>
public enum NotificationAudience
{
    Employee = 0,
    Dispatcher = 1,
    Both = 2
}
