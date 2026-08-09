using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Employees;

/// <summary>
/// A single on-duty record enriched for the employee-detail display (open records on the
/// Work &amp; Staffing tab and the On-Duty History tab). Times are provided both as UTC and as
/// work-area-localized ISO-8601 strings so the front-end renders wall-clock times without any
/// timezone logic of its own.
/// </summary>
public sealed record EmployeeOnDutyRecordItem(
    ControlNumber CtrlNbr,
    decimal       PreviousRestHours,
    string        AssignmentName,
    string        AssignmentCode,
    string        CrewName,
    string        CraftRoleName,
    string        Location,
    long?         WorkAreaCtrlNbr,
    string        WorkAreaName,
    DateTime      OnDutyTimeUtc,
    string        OnDutyLocalIso,
    DateTime?     OffDutyTimeUtc,
    string        OffDutyLocalIso,
    int?          TotalTimeOnDutyMinutes,
    int           ConsecutiveDays,
    bool          IsAssigned,
    bool          IsLateCall,
    string        Status,
    string        CompletionStatus,
    bool          IsQuickTieUp,
    DateTime?     RestedAtUtc,
    bool          OffDutyTimeConfirmed,
    DateTime?     OffDutyTimeConfirmedAtUtc,
    string        OffDutyTimeConfirmedBy,
    string        WorkAreaCode,
    string        EmployeeName,
    string        EmployeeNumber,
    long          EmployeeCtrlNbr,
    long          CraftCtrlNbr,
    string        AssignmentOffDutyLocalIso,
    bool          CanChangeRecord,
    DateTime?     ChangeRecordUntilUtc);

/// <summary>
/// The completed on-duty history windows offered on the employee-detail On-Duty History tab,
/// mirroring the legacy pay-period dropdown. The window bounds for the "work period" options are
/// resolved from the railroad's configured <see cref="Domain.Modules.TenantConfig.WorkPeriodMode"/>.
/// </summary>
public enum OnDutyHistoryPeriod
{
    /// <summary>Completed records within the current work period (legacy value 1).</summary>
    CurrentWorkPeriod = 1,

    /// <summary>Completed records within the previous work period (legacy value 2).</summary>
    PreviousWorkPeriod = 2,

    /// <summary>Completed records within the current calendar month (legacy value 3).</summary>
    CurrentMonth = 3,

    /// <summary>Completed records within the previous calendar month (legacy value 4).</summary>
    PreviousMonth = 4,

    /// <summary>Completed records from the start of the current year to now (legacy value 5).</summary>
    YearToDate = 5
}
