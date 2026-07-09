using CrewService.Application.Modules.UserAccess;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CrewService.Infrastructure.Email;

public class SmtpInvitationEmailService(
    IOptions<SmtpSettings> smtpOptions,
    ILogger<SmtpInvitationEmailService> logger) : IInvitationEmailService
{
    private readonly SmtpSettings _smtp = smtpOptions.Value;

    public async Task SendInvitationAsync(string toEmail, string role, string parentName,
                                          string acceptUrl, DateTime expiresUtc)
    {
        var subject = $"You've been invited to join {parentName} on CrewService";
        var html = BuildInvitationHtml(parentName, role, acceptUrl, expiresUtc);
        var text = BuildInvitationText(parentName, role, acceptUrl, expiresUtc);

        await SendAsync(toEmail, subject, html, text);
    }

    public async Task SendReminderAsync(string toEmail, string role, string parentName,
                                        string acceptUrl, DateTime expiresUtc)
    {
        var subject = $"Reminder: Your invitation to join {parentName} on CrewService";
        var html = BuildInvitationHtml(parentName, role, acceptUrl, expiresUtc, isReminder: true);
        var text = BuildInvitationText(parentName, role, acceptUrl, expiresUtc, isReminder: true);

        await SendAsync(toEmail, subject, html, text);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, string textBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = textBody
        };

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(_smtp.Host, _smtp.Port, _smtp.UseSsl);
            await client.SendAsync(message);
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Invitation email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send invitation email to {Email}", toEmail);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }

    private static string BuildInvitationHtml(string parentName, string role,
        string acceptUrl, DateTime expiresUtc, bool isReminder = false)
    {
        var heading = isReminder
            ? "Reminder: You have a pending invitation"
            : "You've been invited to CrewService";

        return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8" /></head>
            <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5;">
              <div style="max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);">
                <div style="background-color: #0d6efd; padding: 24px 32px;">
                  <h1 style="color: #ffffff; margin: 0; font-size: 24px;">CrewService</h1>
                </div>
                <div style="padding: 32px;">
                  <h2 style="margin-top: 0; color: #333;">{heading}</h2>
                  <p style="color: #555; font-size: 16px; line-height: 1.5;">
                    You have been invited to join <strong>{parentName}</strong> as a <strong>{role}</strong>.
                  </p>
                  <div style="text-align: center; margin: 32px 0;">
                    <a href="{acceptUrl}"
                       style="display: inline-block; padding: 14px 32px; background-color: #0d6efd; color: #ffffff; text-decoration: none; border-radius: 6px; font-size: 16px; font-weight: 600;">
                      Accept Invitation
                    </a>
                  </div>
                  <p style="color: #888; font-size: 14px;">
                    This invitation expires on <strong>{expiresUtc:MMMM d, yyyy}</strong>.
                  </p>
                  <hr style="border: none; border-top: 1px solid #eee; margin: 24px 0;" />
                  <p style="color: #aaa; font-size: 12px;">
                    If you did not expect this invitation, you can safely ignore this email.
                  </p>
                </div>
              </div>
            </body>
            </html>
            """;
    }

    private static string BuildInvitationText(string parentName, string role,
        string acceptUrl, DateTime expiresUtc, bool isReminder = false)
    {
        var heading = isReminder
            ? "Reminder: You have a pending invitation"
            : "You've been invited to CrewService";

        return $"""
            {heading}

            You have been invited to join {parentName} as a {role}.

            Accept your invitation by visiting:
            {acceptUrl}

            This invitation expires on {expiresUtc:MMMM d, yyyy}.

            If you did not expect this invitation, you can safely ignore this email.
            """;
    }
}
