using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Employment;

public sealed class EmploymentStatus : Entity
{
    public ControlNumber ClientCtrlNbr { get; private set; }
    public string StatusCode { get; private set; } = string.Empty;
    public string StatusName { get; private set; } = string.Empty;
    public int StatusNumber { get; private set; }
    public string EmploymentCode { get; private set; } = string.Empty;

    private EmploymentStatus()
    {
        ClientCtrlNbr = null!;
    }

    private EmploymentStatus(
        ControlNumber clientCtrlNbr,
        string statusCode,
        string statusName,
        int statusNumber,
        string employmentCode)
    {
        ClientCtrlNbr = clientCtrlNbr;
        StatusCode = statusCode;
        StatusName = statusName;
        StatusNumber = statusNumber;
        EmploymentCode = employmentCode;
    }

    public static EmploymentStatus Create(
        long clientCtrlNbr,
        string statusCode,
        string statusName,
        int statusNumber,
        string employmentCode)
    {
        return new EmploymentStatus(
            ControlNumber.Create(clientCtrlNbr),
            statusCode,
            statusName,
            statusNumber,
            employmentCode);
    }

    public void Update(string statusCode, string statusName, int statusNumber, string employmentCode)
    {
        StatusCode = statusCode;
        StatusName = statusName;
        StatusNumber = statusNumber;
        EmploymentCode = employmentCode;
    }
}