namespace CrewService.Domain.Interfaces;

public enum NotificationChannel
{
    SystemMessage,
    SystemSupport,
    TieUp,
    ElectronicCall,
    Test
}

public interface IOperationalNotifier
{
    Task SendAsync(NotificationChannel channel, string subject, string body, CancellationToken cancellationToken = default);
}
