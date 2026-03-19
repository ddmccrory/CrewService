namespace CrewService.Application.Modules.UserAccess;

public interface IInvitationEmailService
{
    Task SendInvitationAsync(string toEmail, string role, string parentName,
                             string acceptUrl, DateTime expiresUtc);

    Task SendReminderAsync(string toEmail, string role, string parentName,
                           string acceptUrl, DateTime expiresUtc);
}
