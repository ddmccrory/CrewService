using System.Text.Json;
using CrewService.Application.UserAccess;
using CrewService.Domain.DomainEvents;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.Employees;

public sealed class EmployeeReactiveService(
    InvitationAppService invitationAppService,
    ILogger<EmployeeReactiveService> logger)
{
    public async Task HandleEmployeeCreatedAsync(DomainEvent domainEvent, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(domainEvent.PayloadJson))
        {
            logger.LogWarning("EmployeeReactiveService: EmployeeCreatedDomainEvent has no payload. EventId: {EventId}", domainEvent.EventId);
            return;
        }

        string? email;
        long clientCtrlNbrValue;
        string? invitedByUserId;
        string? invitedByUserName;
        string parentName;

        try
        {
            using var doc = JsonDocument.Parse(domainEvent.PayloadJson);
            var root = doc.RootElement;
            email = root.TryGetProperty("email", out var e) ? e.GetString() : null;
            clientCtrlNbrValue = root.TryGetProperty("clientCtrlNbr", out var c) ? c.GetInt64() : 0;
            invitedByUserId = root.TryGetProperty("invitedByUserId", out var u) ? u.GetString() : null;
            invitedByUserName = root.TryGetProperty("invitedByUserName", out var n) ? n.GetString() : null;
            parentName = root.TryGetProperty("parentName", out var p) ? p.GetString() ?? string.Empty : string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EmployeeReactiveService: Failed to parse payload. EventId: {EventId}", domainEvent.EventId);
            return;
        }

        if (string.IsNullOrEmpty(email) || clientCtrlNbrValue == 0)
        {
            logger.LogWarning("EmployeeReactiveService: Missing email or clientCtrlNbr in payload. EventId: {EventId}", domainEvent.EventId);
            return;
        }

        if (string.IsNullOrEmpty(invitedByUserId) || string.IsNullOrEmpty(invitedByUserName))
        {
            logger.LogError("EmployeeReactiveService: Missing invitedByUserId or invitedByUserName in payload — cannot create invitation without knowing who created the employee. EventId: {EventId}", domainEvent.EventId);
            return;
        }

        try
        {
            var clientCtrlNbr = ControlNumber.Create(clientCtrlNbrValue);
            await invitationAppService.CreateFromSystemAsync(email, clientCtrlNbr, "Employee", invitedByUserId, invitedByUserName, parentName, 30, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EmployeeReactiveService: Failed to create invitation for {Email}. Error: {Message} | Inner: {Inner}",
                email, ex.Message, ex.InnerException?.Message);
        }
    }
}
