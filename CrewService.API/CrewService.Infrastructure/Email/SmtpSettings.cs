namespace CrewService.Infrastructure.Email;

public class SmtpSettings
{
    public const string SectionName = "SmtpSettings";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public string FromAddress { get; set; } = "noreply@crewservice.local";
    public string FromName { get; set; } = "CrewService";
    public bool UseSsl { get; set; } = false;
}
