using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Persistance.Repositories;
using CrewService.UnitTests.Fixtures;

namespace CrewService.UnitTests.AbsenceVacancy;

public sealed class AbsenceRequestRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetByDateRangeAsync_IncludesRailroadAndParentScopedEmployees()
    {
        var ct = TestContext.Current.CancellationToken;
        using var context = _factory.CreateContext();

        var parent = Parent.Create("Parent A");
        var otherParent = Parent.Create("Parent B");
        context.Parents.AddRange(parent, otherParent);

        var groupType = GroupType.Create("Railroad", "Railroad", isWorkArea: true);
        context.Set<GroupType>().Add(groupType);
        await context.SaveChangesAsync(ct);

        var railroad = DynamicGroup.Create(
            groupType.CtrlNbr,
            "Test Railroad",
            parentGroupCtrlNbr: null,
            path: null,
            isWorkArea: false,
            code: "TRR",
            parentCtrlNbr: parent.CtrlNbr);
        context.DynamicGroups.Add(railroad);
        await context.SaveChangesAsync(ct);

        var parentStatus = EmploymentStatus.Create(parent.CtrlNbr, "ACT", "Active", 1, "A");
        var railroadStatus = EmploymentStatus.Create(railroad.CtrlNbr, "ACT", "Active", 1, "A");
        var otherStatus = EmploymentStatus.Create(otherParent.CtrlNbr, "ACT", "Active", 1, "A");
        context.EmploymentStatuses.AddRange(parentStatus, railroadStatus, otherStatus);
        await context.SaveChangesAsync(ct);

        var railroadEmployee = CreateEmployee(railroad.CtrlNbr, railroadStatus.CtrlNbr, "RR001", "rr001", "000-00-0001");
        var parentEmployee = CreateEmployee(parent.CtrlNbr, parentStatus.CtrlNbr, "PA001", "pa001", "000-00-0002");
        var otherEmployee = CreateEmployee(otherParent.CtrlNbr, otherStatus.CtrlNbr, "OB001", "ob001", "000-00-0003");
        context.Employees.AddRange(railroadEmployee, parentEmployee, otherEmployee);
        await context.SaveChangesAsync(ct);

        var dayStart = new DateTime(2026, 7, 31, 5, 0, 0, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        context.Set<AbsenceRequest>().AddRange(
            AbsenceRequest.Create(railroadEmployee.CtrlNbr, dayStart.AddMinutes(1), null, "MARKOFF"),
            AbsenceRequest.Create(parentEmployee.CtrlNbr, dayStart.AddHours(2), null, "MARKOFF"),
            AbsenceRequest.Create(otherEmployee.CtrlNbr, dayStart.AddHours(3), null, "MARKOFF"));
        await context.SaveChangesAsync(ct);

        var repository = new AbsenceRequestRepository(context, _factory.CurrentUserService);

        var results = await repository.GetByDateRangeAsync(railroad.CtrlNbr, dayStart, dayEnd, includeAllStatuses: true, ct);

        Assert.Equal(2, results.Count);
        var employeeIds = results.Select(r => r.EmployeeCtrlNbr.Value).ToHashSet();
        Assert.Contains(parentEmployee.CtrlNbr.Value, employeeIds);
        Assert.Contains(railroadEmployee.CtrlNbr.Value, employeeIds);
    }

    [Fact]
    public async Task GetByDateRangeAsync_UsesInclusiveStartAndExclusiveEndBoundaries()
    {
        var ct = TestContext.Current.CancellationToken;
        using var context = _factory.CreateContext();

        var parent = Parent.Create("Parent A");
        context.Parents.Add(parent);

        var groupType = GroupType.Create("Railroad", "Railroad", isWorkArea: true);
        context.Set<GroupType>().Add(groupType);
        await context.SaveChangesAsync(ct);

        var railroad = DynamicGroup.Create(
            groupType.CtrlNbr,
            "Test Railroad",
            parentGroupCtrlNbr: null,
            path: null,
            isWorkArea: false,
            code: "TRR",
            parentCtrlNbr: parent.CtrlNbr);
        context.DynamicGroups.Add(railroad);
        await context.SaveChangesAsync(ct);

        var status = EmploymentStatus.Create(railroad.CtrlNbr, "ACT", "Active", 1, "A");
        context.EmploymentStatuses.Add(status);
        await context.SaveChangesAsync(ct);

        var employee = CreateEmployee(railroad.CtrlNbr, status.CtrlNbr, "RR001", "rr001", "000-00-0004");
        context.Employees.Add(employee);
        await context.SaveChangesAsync(ct);

        var rangeStart = new DateTime(2026, 7, 31, 5, 0, 0, DateTimeKind.Utc);
        var rangeEnd = rangeStart.AddDays(1);

        context.Set<AbsenceRequest>().AddRange(
            AbsenceRequest.Create(employee.CtrlNbr, rangeStart, null, "MARKOFF"),
            AbsenceRequest.Create(employee.CtrlNbr, rangeEnd, null, "MARKOFF"));
        await context.SaveChangesAsync(ct);

        var repository = new AbsenceRequestRepository(context, _factory.CurrentUserService);

        var results = await repository.GetByDateRangeAsync(railroad.CtrlNbr, rangeStart, rangeEnd, includeAllStatuses: true, ct);

        var result = Assert.Single(results);
        Assert.Equal(rangeStart, result.StartUtc);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ExcludeDeniedAndCancelled_WhenIncludeAllStatusesIsFalse()
    {
        var ct = TestContext.Current.CancellationToken;
        using var context = _factory.CreateContext();

        var parent = Parent.Create("Parent A");
        context.Parents.Add(parent);

        var groupType = GroupType.Create("Railroad", "Railroad", isWorkArea: true);
        context.Set<GroupType>().Add(groupType);
        await context.SaveChangesAsync(ct);

        var railroad = DynamicGroup.Create(
            groupType.CtrlNbr,
            "Test Railroad",
            parentGroupCtrlNbr: null,
            path: null,
            isWorkArea: false,
            code: "TRR",
            parentCtrlNbr: parent.CtrlNbr);
        context.DynamicGroups.Add(railroad);
        await context.SaveChangesAsync(ct);

        var status = EmploymentStatus.Create(railroad.CtrlNbr, "ACT", "Active", 1, "A");
        context.EmploymentStatuses.Add(status);
        await context.SaveChangesAsync(ct);

        var employee = CreateEmployee(railroad.CtrlNbr, status.CtrlNbr, "RR001", "rr001", "000-00-0005");
        context.Employees.Add(employee);
        await context.SaveChangesAsync(ct);

        var rangeStart = new DateTime(2026, 7, 31, 5, 0, 0, DateTimeKind.Utc);
        var rangeEnd = rangeStart.AddDays(1);

        var pending = AbsenceRequest.Create(employee.CtrlNbr, rangeStart.AddMinutes(1), null, "MARKOFF");
        var denied = AbsenceRequest.Create(employee.CtrlNbr, rangeStart.AddHours(1), null, "MARKOFF");
        denied.Deny(employee.CtrlNbr);
        var cancelled = AbsenceRequest.Create(employee.CtrlNbr, rangeStart.AddHours(2), null, "MARKOFF");
        cancelled.Cancel();

        context.Set<AbsenceRequest>().AddRange(pending, denied, cancelled);
        await context.SaveChangesAsync(ct);

        var repository = new AbsenceRequestRepository(context, _factory.CurrentUserService);

        var results = await repository.GetByDateRangeAsync(railroad.CtrlNbr, rangeStart, rangeEnd, includeAllStatuses: false, ct);

        var result = Assert.Single(results);
        Assert.Equal("PENDING", result.Status);
    }

    private static Employee CreateEmployee(
        CrewService.Domain.ValueObjects.ControlNumber clientCtrlNbr,
        CrewService.Domain.ValueObjects.ControlNumber employmentStatusCtrlNbr,
        string employeeNumber,
        string userId,
        string ssn)
    {
        return Employee.Create(
            clientCtrlNbr,
            userId,
            employeeNumber,
            ssn,
            Gender.Female,
            Race.White,
            new DateTime(1990, 1, 1),
            DateTime.UtcNow.Date,
            employmentStatusCtrlNbr,
            $"{userId}@example.com",
            "system",
            "System");
    }
}