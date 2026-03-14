using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Payroll;

public sealed class HolidayPayrollRecord : Entity
{
    public ControlNumber HolidayCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber? PayrollRecordCtrlNbr { get; private set; }
    public bool IsQualified { get; private set; }
    public string? DisqualificationReason { get; private set; }

    private HolidayPayrollRecord()
    {
        HolidayCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    public static HolidayPayrollRecord Create(
        ControlNumber holidayCtrlNbr, ControlNumber employeeCtrlNbr,
        bool isQualified, string? disqualificationReason = null,
        ControlNumber? payrollRecordCtrlNbr = null)
    {
        return new HolidayPayrollRecord
        {
            HolidayCtrlNbr = holidayCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            IsQualified = isQualified,
            DisqualificationReason = disqualificationReason,
            PayrollRecordCtrlNbr = payrollRecordCtrlNbr
        };
    }
}
