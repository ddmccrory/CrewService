using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.AbsenceVacancy;

public class AbsenceWaitListTests
{
    [Fact]
    public void CreateCompensableDay_SetsDeterministicFields()
    {
        var requestDateUtc = new DateTime(2026, 08, 30, 13, 45, 00, DateTimeKind.Utc);
        var entryUtc = new DateTime(2026, 08, 30, 14, 00, 00, DateTimeKind.Utc);

        var record = AbsenceRequestWaitListRecord.CreateCompensableDay(
            employeeCtrlNbr: ControlNumber.Create(100),
            absenceCodeCtrlNbr: ControlNumber.Create(200),
            requestDateUtc: requestDateUtc,
            entryUtc: entryUtc,
            craftCtrlNbr: ControlNumber.Create(300),
            departmentCtrlNbr: ControlNumber.Create(400));

        Assert.Equal(100, record.EmployeeCtrlNbr.Value);
        Assert.Equal(200, record.AbsenceCodeCtrlNbr.Value);
        Assert.Equal(new DateTime(2026, 08, 30, 0, 0, 0, DateTimeKind.Utc), record.RequestDateUtc);
        Assert.Equal(entryUtc, record.EntryUtc);
        Assert.Equal(AbsenceRequestWaitListType.CompensableDay, record.WaitListType);
        Assert.Equal(300, record.CraftCtrlNbr!.Value);
        Assert.Equal(400, record.DepartmentCtrlNbr!.Value);
        Assert.Null(record.AssignedAtUtc);
    }

    [Fact]
    public void CreateVacationWeek_SetsVacationWeekType()
    {
        var record = AbsenceRequestWaitListRecord.CreateVacationWeek(
            employeeCtrlNbr: ControlNumber.Create(100),
            absenceCodeCtrlNbr: ControlNumber.Create(200),
            requestDateUtc: new DateTime(2026, 09, 01, 0, 0, 0, DateTimeKind.Utc),
            entryUtc: new DateTime(2026, 09, 01, 0, 0, 0, DateTimeKind.Utc),
            craftCtrlNbr: null,
            departmentCtrlNbr: null);

        Assert.Equal(AbsenceRequestWaitListType.VacationWeek, record.WaitListType);
    }

    [Fact]
    public void MarkAssigned_CanOnlyBeSetOnce()
    {
        var record = AbsenceRequestWaitListRecord.CreateCompensableDay(
            employeeCtrlNbr: ControlNumber.Create(100),
            absenceCodeCtrlNbr: ControlNumber.Create(200),
            requestDateUtc: new DateTime(2026, 08, 30, 0, 0, 0, DateTimeKind.Utc),
            entryUtc: new DateTime(2026, 08, 30, 0, 5, 0, DateTimeKind.Utc),
            craftCtrlNbr: null,
            departmentCtrlNbr: null);

        var assignedAtUtc = new DateTime(2026, 08, 30, 10, 0, 0, DateTimeKind.Utc);
        record.MarkAssigned(assignedAtUtc, "Assigned");

        Assert.Equal(assignedAtUtc, record.AssignedAtUtc);
        Assert.Equal("Assigned", record.AssignmentNotes);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            record.MarkAssigned(assignedAtUtc.AddMinutes(1), "Assigned again"));

        Assert.Equal("Waitlist record is already assigned.", ex.Message);
    }

    [Fact]
    public void AbsenceWaitListAllowancePolicy_Create_NormalizesAndSetsValues()
    {
        var policy = AbsenceWaitListAllowancePolicy.Create(
            craftCtrlNbr: ControlNumber.Create(700),
            waitListType: " vacation_week ",
            allowanceCode: " vw ",
            calendarYear: 2026,
            maxAssignments: 12,
            isEnabled: true);

        Assert.Equal(700, policy.CraftCtrlNbr.Value);
        Assert.Equal("VACATION_WEEK", policy.WaitListType);
        Assert.Equal("VW", policy.AllowanceCode);
        Assert.Equal(2026, policy.CalendarYear);
        Assert.Equal(12, policy.MaxAssignments);
        Assert.True(policy.IsEnabled);
    }
}
