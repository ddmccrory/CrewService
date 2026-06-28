using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.VacancyCalls;

public sealed class VacancyCallRequest : Entity
{
    private readonly List<VacancyCallResponse> _responses = [];

    public ControlNumber PositionSlotCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public string TemplateType { get; private set; } = string.Empty;
    public DateTime SentAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public string Status { get; private set; } = "Sent";
    public string? ExternalId { get; private set; }

    public IReadOnlyList<VacancyCallResponse> Responses => _responses.AsReadOnly();

    private VacancyCallRequest()
    {
        PositionSlotCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    public static VacancyCallRequest Create(
        ControlNumber positionSlotCtrlNbr,
        ControlNumber employeeCtrlNbr,
        string templateType,
        int pollingTimeoutMinutes = 6,
        string? externalId = null)
    {
        var now = DateTime.UtcNow;
        return new VacancyCallRequest
        {
            PositionSlotCtrlNbr = positionSlotCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            TemplateType = templateType,
            SentAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(pollingTimeoutMinutes),
            ExternalId = externalId
        };
    }

    public VacancyCallResponse RecordResponse(string responseType, string? deviceType = null)
    {
        var response = VacancyCallResponse.Create(CtrlNbr, responseType, deviceType);
        _responses.Add(response);
        Status = responseType == "Accept" ? "Accepted" : "Rejected";
        return response;
    }

    public void MarkExpired()
    {
        Status = "Expired";
    }

    public void MarkFailed()
    {
        Status = "Failed";
    }

    public bool IsExpired() => DateTime.UtcNow >= ExpiresAtUtc && Status == "Sent";
}

public sealed class VacancyCallResponse : Entity
{
    public ControlNumber VacancyCallRequestCtrlNbr { get; private set; }
    public string ResponseType { get; private set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; private set; }
    public string? DeviceType { get; private set; }

    private VacancyCallResponse() { VacancyCallRequestCtrlNbr = null!; }

    internal static VacancyCallResponse Create(
        ControlNumber vacancyCallRequestCtrlNbr, string responseType, string? deviceType)
    {
        return new VacancyCallResponse
        {
            VacancyCallRequestCtrlNbr = vacancyCallRequestCtrlNbr,
            ResponseType = responseType,
            ReceivedAtUtc = DateTime.UtcNow,
            DeviceType = deviceType
        };
    }
}
