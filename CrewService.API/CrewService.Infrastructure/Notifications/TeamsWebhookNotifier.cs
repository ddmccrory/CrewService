using System.Net.Http.Json;
using CrewService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CrewService.Infrastructure.Notifications;

internal sealed class TeamsWebhookNotifier(
    IHttpClientFactory httpClientFactory,
    ILogger<TeamsWebhookNotifier> logger) : IOperationalNotifier
{
    public async Task SendAsync(
        NotificationChannel channel,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        // Webhook URL resolution is handled by the caller or a decorator
        // that reads TeamsWebhookConfig from the database.
        // This implementation posts to a URL provided via named HttpClient.
        var client = httpClientFactory.CreateClient($"TeamsWebhook_{channel}");

        if (client.BaseAddress is null)
        {
            logger.LogWarning("No webhook URL configured for channel {Channel}; skipping notification", channel);
            return;
        }

        var payload = new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    contentUrl = (string?)null,
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = new object[]
                        {
                            new { type = "TextBlock", size = "Medium", weight = "Bolder", text = subject },
                            new { type = "TextBlock", text = body, wrap = true }
                        }
                    }
                }
            }
        };

        try
        {
            var response = await client.PostAsJsonAsync(string.Empty, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "Teams webhook for channel {Channel} returned {StatusCode}: {Body}",
                    channel, response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send Teams notification to channel {Channel}", channel);
        }
    }
}
