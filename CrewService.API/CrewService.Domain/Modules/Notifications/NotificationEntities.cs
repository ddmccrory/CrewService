using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Notifications;

public sealed class NotificationRequest : Entity
{
    private readonly List<NotificationResponse> _responses = [];

    public ControlNumber PositionSlotCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public string TemplateType { get; private set; } = string.Empty;
    public DateTime SentAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public string Status { get; private set; } = "Sent";
    public string? ExternalId { get; private set; }

    public IReadOnlyList<NotificationResponse> Responses => _responses.AsReadOnly();

    private NotificationRequest()
    {
        PositionSlotCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    public static NotificationRequest Create(
        ControlNumber positionSlotCtrlNbr,
        ControlNumber employeeCtrlNbr,
        string templateType,
        int pollingTimeoutMinutes = 6,
        string? externalId = null)
    {
        var now = DateTime.UtcNow;
        return new NotificationRequest
        {
            PositionSlotCtrlNbr = positionSlotCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            TemplateType = templateType,
            SentAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(pollingTimeoutMinutes),
            ExternalId = externalId,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }

    public NotificationResponse RecordResponse(string responseType, string? deviceType = null)
    {
        var response = NotificationResponse.Create(CtrlNbr, responseType, deviceType);
        _responses.Add(response);
        Status = responseType == "Accept" ? "Accepted" : "Rejected";
        ModifiedBy = AuditStamp.Create("SYSTEM");
        return response;
    }

    public void MarkExpired()
    {
        Status = "Expired";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public void MarkFailed()
    {
        Status = "Failed";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public bool IsExpired() => DateTime.UtcNow >= ExpiresAtUtc && Status == "Sent";
}

public sealed class NotificationResponse : Entity
{
    public ControlNumber NotificationRequestCtrlNbr { get; private set; }
    public string ResponseType { get; private set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; private set; }
    public string? DeviceType { get; private set; }

    private NotificationResponse() { NotificationRequestCtrlNbr = null!; }

    internal static NotificationResponse Create(
        ControlNumber notificationRequestCtrlNbr, string responseType, string? deviceType)
    {
        return new NotificationResponse
        {
            NotificationRequestCtrlNbr = notificationRequestCtrlNbr,
            ResponseType = responseType,
            ReceivedAtUtc = DateTime.UtcNow,
            DeviceType = deviceType,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }
}
