using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
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

        var results = await repository.GetByDateRangeAsync(railroad.CtrlNbr, dayStart, dayEnd, includeAllStatuses: true, ct: ct);

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

        var results = await repository.GetByDateRangeAsync(railroad.CtrlNbr, rangeStart, rangeEnd, includeAllStatuses: true, ct: ct);

        var result = Assert.Single(results);
        Assert.Equal(rangeStart, result.ScheduledStartUtc);
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

        var results = await repository.GetByDateRangeAsync(railroad.CtrlNbr, rangeStart, rangeEnd, includeAllStatuses: false, ct: ct);

        var result = Assert.Single(results);
        Assert.Equal("PENDING", result.DerivedStatus);
    }

    [Fact]
    public async Task GetOpenAbsencesByRangeAsync_ExcludesApprovedRequests()
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

        var employee = CreateEmployee(railroad.CtrlNbr, status.CtrlNbr, "RR001", "rr001", "000-00-0010");
        context.Employees.Add(employee);
        await context.SaveChangesAsync(ct);

        var nowUtc = DateTime.UtcNow;
        var approved = AbsenceRequest.Create(employee.CtrlNbr, nowUtc.AddHours(-1), null, "MARKOFF");
        approved.Approve(employee.CtrlNbr);

        context.Set<AbsenceRequest>().Add(approved);
        await context.SaveChangesAsync(ct);

        var repository = new AbsenceRequestRepository(context, _factory.CurrentUserService);

        var results = await repository.GetOpenAbsencesByRangeAsync(
            railroad.CtrlNbr,
            nowUtc.AddDays(-1),
            nowUtc.AddDays(1),
            ct: ct);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetOpenAbsencesByRangeAsync_IncludesExercisedWithoutMarkup()
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

        var employee = CreateEmployee(railroad.CtrlNbr, status.CtrlNbr, "RR001", "rr001", "000-00-0110");
        context.Employees.Add(employee);
        await context.SaveChangesAsync(ct);

        var exercisedStart = DateTime.UtcNow.AddDays(-3);
        var exercised = AbsenceRequest.Create(employee.CtrlNbr, exercisedStart.AddHours(-1), null, "MARKOFF");
        exercised.Approve(employee.CtrlNbr);
        exercised.Exercise(exercisedStart);

        context.Set<AbsenceRequest>().Add(exercised);
        await context.SaveChangesAsync(ct);

        var repository = new AbsenceRequestRepository(context, _factory.CurrentUserService);

        var results = await repository.GetOpenAbsencesByRangeAsync(
            railroad.CtrlNbr,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(1),
            ct: ct);

        var result = Assert.Single(results);
        Assert.Equal(exercised.CtrlNbr, result.CtrlNbr);
        Assert.Equal("OPEN", result.DerivedStatus);
    }

    [Fact]
    public async Task GetOpenAbsencesByRangeAsync_ExcludesExercisedWithoutMarkupWhenEndHasPassed()
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

        var employee = CreateEmployee(railroad.CtrlNbr, status.CtrlNbr, "RR001", "rr001", "000-00-0111");
        context.Employees.Add(employee);
        await context.SaveChangesAsync(ct);

        var exercised = AbsenceRequest.Create(employee.CtrlNbr, DateTime.UtcNow.AddDays(-5), null, "MARKOFF");
        exercised.Approve(employee.CtrlNbr);
        exercised.Exercise(DateTime.UtcNow.AddDays(-5));
        exercised.CompleteByMarkUp(DateTime.UtcNow.AddHours(-2));

        context.Set<AbsenceRequest>().Add(exercised);
        await context.SaveChangesAsync(ct);

        var repository = new AbsenceRequestRepository(context, _factory.CurrentUserService);

        var results = await repository.GetOpenAbsencesByRangeAsync(
            railroad.CtrlNbr,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(1),
            ct: ct);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByDateRangeAsync_FiltersNoSlotRequestsByActiveSeniorityCraftAndDepartment()
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

        var clericalEmployee = CreateEmployee(railroad.CtrlNbr, status.CtrlNbr, "RR020", "rr020", "000-00-0020");
        var transportationEmployee = CreateEmployee(railroad.CtrlNbr, status.CtrlNbr, "RR021", "rr021", "000-00-0021");
        context.Employees.AddRange(clericalEmployee, transportationEmployee);
        await context.SaveChangesAsync(ct);

        var clericalDepartment = Department.Create(parent.CtrlNbr, railroad.CtrlNbr, "Clerical");
        var transportationDepartment = Department.Create(parent.CtrlNbr, railroad.CtrlNbr, "Transportation");
        context.Set<Department>().AddRange(clericalDepartment, transportationDepartment);

        var clericalCraft = Craft.Create(
            parent.CtrlNbr,
            railroad.CtrlNbr,
            "Clerical",
            "Clericals",
            1,
            autoMarkUp: false,
            approveAllMarkOffs: false,
            markOffHours: 0,
            markUpHours: 0,
            requiredRestHours: 0,
            maximumVacationDayTime: 0,
            unpaidMealPeriodMinutes: 0,
            hoursofService: false,
            processPayroll: false,
            showNotifications: false,
            vacationAssignmentType: 0,
            departmentCtrlNbr: clericalDepartment.CtrlNbr);

        var transportationCraft = Craft.Create(
            parent.CtrlNbr,
            railroad.CtrlNbr,
            "Transportation",
            "Transportations",
            2,
            autoMarkUp: false,
            approveAllMarkOffs: false,
            markOffHours: 0,
            markUpHours: 0,
            requiredRestHours: 0,
            maximumVacationDayTime: 0,
            unpaidMealPeriodMinutes: 0,
            hoursofService: false,
            processPayroll: false,
            showNotifications: false,
            vacationAssignmentType: 0,
            departmentCtrlNbr: transportationDepartment.CtrlNbr);

        context.Set<Craft>().AddRange(clericalCraft, transportationCraft);
        await context.SaveChangesAsync(ct);

        var clericalRoster = Roster.Create(clericalCraft.CtrlNbr, railroad.CtrlNbr, null, "Clerical", "Clericals", 1);
        var transportationRoster = Roster.Create(transportationCraft.CtrlNbr, railroad.CtrlNbr, null, "Transportation", "Transportations", 2);
        context.Set<Roster>().AddRange(clericalRoster, transportationRoster);

        var activeState = SeniorityState.Create("Active", StateType.Active, parent.CtrlNbr.Value);
        context.Set<SeniorityState>().Add(activeState);
        await context.SaveChangesAsync(ct);

        var rosterDate = new DateTime(2026, 7, 30, 0, 0, 0, DateTimeKind.Utc);
        context.Set<Seniority>().AddRange(
            Seniority.Create(clericalRoster.CtrlNbr, clericalEmployee.CtrlNbr, lastActiveRoster: true, rosterDate, rank: 1, activeState.CtrlNbr, canTrain: false),
            Seniority.Create(transportationRoster.CtrlNbr, transportationEmployee.CtrlNbr, lastActiveRoster: true, rosterDate, rank: 1, activeState.CtrlNbr, canTrain: false));
        await context.SaveChangesAsync(ct);

        var rangeStart = new DateTime(2026, 7, 31, 5, 0, 0, DateTimeKind.Utc);
        var rangeEnd = rangeStart.AddDays(1);
        var clericalRequest = AbsenceRequest.Create(clericalEmployee.CtrlNbr, rangeStart.AddHours(1), null, "MARKOFF");
        var transportationRequest = AbsenceRequest.Create(transportationEmployee.CtrlNbr, rangeStart.AddHours(2), null, "MARKOFF");
        clericalRequest.Approve(clericalEmployee.CtrlNbr);
        transportationRequest.Approve(transportationEmployee.CtrlNbr);

        context.Set<AbsenceRequest>().AddRange(clericalRequest, transportationRequest);
        await context.SaveChangesAsync(ct);

        var repository = new AbsenceRequestRepository(context, _factory.CurrentUserService);

        var craftFiltered = await repository.GetByDateRangeAsync(
            railroad.CtrlNbr,
            rangeStart,
            rangeEnd,
            includeAllStatuses: true,
            craftCtrlNbr: clericalCraft.CtrlNbr,
            ct: ct);

        var departmentFiltered = await repository.GetByDateRangeAsync(
            railroad.CtrlNbr,
            rangeStart,
            rangeEnd,
            includeAllStatuses: true,
            departmentCtrlNbr: clericalDepartment.CtrlNbr,
            ct: ct);

        Assert.Single(craftFiltered);
        Assert.Equal(clericalEmployee.CtrlNbr, craftFiltered[0].EmployeeCtrlNbr);

        Assert.Single(departmentFiltered);
        Assert.Equal(clericalEmployee.CtrlNbr, departmentFiltered[0].EmployeeCtrlNbr);
    }

    [Fact]
    public async Task GetByDateRangeAsync_DepartmentFilterUsesEmployeeCraftForCreateWithCodeRequests()
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

        var employee = CreateEmployee(railroad.CtrlNbr, status.CtrlNbr, "RR030", "rr030", "000-00-0030");
        context.Employees.Add(employee);
        await context.SaveChangesAsync(ct);

        var clericalDepartment = Department.Create(parent.CtrlNbr, railroad.CtrlNbr, "Clerical");
        var transportationDepartment = Department.Create(parent.CtrlNbr, railroad.CtrlNbr, "Transportation");
        context.Set<Department>().AddRange(clericalDepartment, transportationDepartment);

        var clericalCraft = Craft.Create(
            parent.CtrlNbr,
            railroad.CtrlNbr,
            "Clerical",
            "Clericals",
            1,
            autoMarkUp: false,
            approveAllMarkOffs: false,
            markOffHours: 0,
            markUpHours: 0,
            requiredRestHours: 0,
            maximumVacationDayTime: 0,
            unpaidMealPeriodMinutes: 0,
            hoursofService: false,
            processPayroll: false,
            showNotifications: false,
            vacationAssignmentType: 0,
            departmentCtrlNbr: clericalDepartment.CtrlNbr);
        context.Set<Craft>().Add(clericalCraft);

        var roster = Roster.Create(clericalCraft.CtrlNbr, railroad.CtrlNbr, null, "Clerical", "Clericals", 1);
        context.Set<Roster>().Add(roster);

        var activeState = SeniorityState.Create("Active", StateType.Active, parent.CtrlNbr.Value);
        context.Set<SeniorityState>().Add(activeState);
        await context.SaveChangesAsync(ct);

        var rosterDate = new DateTime(2026, 7, 30, 0, 0, 0, DateTimeKind.Utc);
        context.Set<Seniority>().Add(
            Seniority.Create(roster.CtrlNbr, employee.CtrlNbr, lastActiveRoster: true, rosterDate, rank: 1, activeState.CtrlNbr, canTrain: false));
        await context.SaveChangesAsync(ct);

        var absenceCode = AbsenceCode.Create(
            railroad.CtrlNbr.Value,
            "MK",
            "Mark Off",
            isExcused: true,
            isCompensated: false,
            requiresApproval: true,
            isSystemOnly: false,
            isHolidayExempt: false,
            defaultAutoMarkUpHours: null,
            isActive: true);
        context.Set<AbsenceCode>().Add(absenceCode);
        await context.SaveChangesAsync(ct);

        var rangeStart = new DateTime(2026, 7, 31, 5, 0, 0, DateTimeKind.Utc);
        var rangeEnd = rangeStart.AddDays(1);
        var request = AbsenceRequest.CreateWithCode(
            employee.CtrlNbr,
            rangeStart.AddHours(1),
            null,
            absenceCodeCtrlNbr: absenceCode.CtrlNbr,
            reasonCode: "MARKOFF",
            isSystemGenerated: false);

        context.Set<AbsenceRequest>().Add(request);
        await context.SaveChangesAsync(ct);

        var repository = new AbsenceRequestRepository(context, _factory.CurrentUserService);

        var clericalResults = await repository.GetByDateRangeAsync(
            railroad.CtrlNbr,
            rangeStart,
            rangeEnd,
            includeAllStatuses: true,
            departmentCtrlNbr: clericalDepartment.CtrlNbr,
            ct: ct);

        var transportationResults = await repository.GetByDateRangeAsync(
            railroad.CtrlNbr,
            rangeStart,
            rangeEnd,
            includeAllStatuses: true,
            departmentCtrlNbr: transportationDepartment.CtrlNbr,
            ct: ct);

        Assert.Single(clericalResults);
        Assert.Equal(employee.CtrlNbr, clericalResults[0].EmployeeCtrlNbr);
        Assert.Empty(transportationResults);
    }

    [Fact]
    public async Task GetApprovedAutoMarkOffDueAsync_ReturnsOnlyApprovedDueAutoMarkOffRequests()
    {
        var ct = TestContext.Current.CancellationToken;
        using var context = _factory.CreateContext();

        var nowUtc = DateTime.UtcNow;
        var parent = Parent.Create("Parent AutoMarkOff Due");
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

        var employee = CreateEmployee(
            clientCtrlNbr: railroad.CtrlNbr,
            employmentStatusCtrlNbr: status.CtrlNbr,
            employeeNumber: "RR090",
            userId: "rr090",
            ssn: "000-00-0090");
        context.Employees.Add(employee);
        await context.SaveChangesAsync(ct);

        var absenceCode = AbsenceCode.Create(
            railroad.CtrlNbr.Value,
            "MK",
            "Mark Off",
            isExcused: true,
            isCompensated: false,
            requiresApproval: true,
            isSystemOnly: false,
            isHolidayExempt: false,
            defaultAutoMarkUpHours: null,
            isActive: true);
        context.Set<AbsenceCode>().Add(absenceCode);
        await context.SaveChangesAsync(ct);

        var due = AbsenceRequest.CreateWithCode(
            employee.CtrlNbr,
            nowUtc.AddMinutes(-5),
            null,
            absenceCode.CtrlNbr,
            "MARKOFF",
            autoMarkOffOnApproval: true);
        due.Approve(employee.CtrlNbr);

        var notDue = AbsenceRequest.CreateWithCode(
            employee.CtrlNbr,
            nowUtc.AddHours(2),
            null,
            absenceCode.CtrlNbr,
            "MARKOFF",
            autoMarkOffOnApproval: true);
        notDue.Approve(employee.CtrlNbr);

        var flagOff = AbsenceRequest.CreateWithCode(
            employee.CtrlNbr,
            nowUtc.AddMinutes(-2),
            null,
            absenceCode.CtrlNbr,
            "MARKOFF",
            autoMarkOffOnApproval: false);
        flagOff.Approve(employee.CtrlNbr);

        context.Set<AbsenceRequest>().AddRange(due, notDue, flagOff);
        await context.SaveChangesAsync(ct);

        var repository = new AbsenceRequestRepository(context, _factory.CurrentUserService);
        var results = await repository.GetApprovedAutoMarkOffDueAsync(nowUtc, ct);

        var request = Assert.Single(results);
        Assert.Equal(due.CtrlNbr, request.CtrlNbr);
    }

    [Fact]
    public async Task GetNextApprovedAutoMarkOffStartUtcAsync_ReturnsEarliestApprovedAutoMarkOffStart()
    {
        var ct = TestContext.Current.CancellationToken;
        using var context = _factory.CreateContext();

        var nowUtc = DateTime.UtcNow;
        var parent = Parent.Create("Parent AutoMarkOff Next");
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

        var employee = CreateEmployee(
            clientCtrlNbr: railroad.CtrlNbr,
            employmentStatusCtrlNbr: status.CtrlNbr,
            employeeNumber: "RR091",
            userId: "rr091",
            ssn: "000-00-0091");
        context.Employees.Add(employee);
        await context.SaveChangesAsync(ct);

        var absenceCode = AbsenceCode.Create(
            railroad.CtrlNbr.Value,
            "MK",
            "Mark Off",
            isExcused: true,
            isCompensated: false,
            requiresApproval: true,
            isSystemOnly: false,
            isHolidayExempt: false,
            defaultAutoMarkUpHours: null,
            isActive: true);
        context.Set<AbsenceCode>().Add(absenceCode);
        await context.SaveChangesAsync(ct);

        var later = AbsenceRequest.CreateWithCode(
            employee.CtrlNbr,
            nowUtc.AddHours(4),
            null,
            absenceCode.CtrlNbr,
            "MARKOFF",
            autoMarkOffOnApproval: true);
        later.Approve(employee.CtrlNbr);

        var earliest = AbsenceRequest.CreateWithCode(
            employee.CtrlNbr,
            nowUtc.AddMinutes(20),
            null,
            absenceCode.CtrlNbr,
            "MARKOFF",
            autoMarkOffOnApproval: true);
        earliest.Approve(employee.CtrlNbr);

        var alreadyExercised = AbsenceRequest.CreateWithCode(
            employee.CtrlNbr,
            nowUtc.AddMinutes(10),
            null,
            absenceCode.CtrlNbr,
            "MARKOFF",
            autoMarkOffOnApproval: true);
        alreadyExercised.Approve(employee.CtrlNbr);
        alreadyExercised.Exercise(nowUtc.AddMinutes(10));

        context.Set<AbsenceRequest>().AddRange(later, earliest, alreadyExercised);
        await context.SaveChangesAsync(ct);

        var repository = new AbsenceRequestRepository(context, _factory.CurrentUserService);
        var next = await repository.GetNextApprovedAutoMarkOffStartUtcAsync(ct);

        Assert.NotNull(next);
        Assert.Equal(DateTime.SpecifyKind(earliest.ScheduledStartUtc, DateTimeKind.Utc), DateTime.SpecifyKind(next.Value, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetPendingByDateAsync_OrdersByEntryUtcThenCtrlNbr()
    {
        var ct = TestContext.Current.CancellationToken;
        using var context = _factory.CreateContext();

        var parent = Parent.Create("Parent WaitList");
        context.Parents.Add(parent);

        var groupType = GroupType.Create("Railroad", "Railroad", isWorkArea: true);
        context.Set<GroupType>().Add(groupType);
        await context.SaveChangesAsync(ct);

        var railroad = DynamicGroup.Create(
            groupType.CtrlNbr,
            "WaitList Railroad",
            parentGroupCtrlNbr: null,
            path: null,
            isWorkArea: false,
            code: "WLR",
            parentCtrlNbr: parent.CtrlNbr);
        context.DynamicGroups.Add(railroad);

        var status = EmploymentStatus.Create(railroad.CtrlNbr, "ACT", "Active", 1, "A");
        context.EmploymentStatuses.Add(status);
        await context.SaveChangesAsync(ct);

        var employee = CreateEmployee(railroad.CtrlNbr, status.CtrlNbr, "RR120", "rr120", "000-00-0120");
        context.Employees.Add(employee);

        var department = Department.Create(parent.CtrlNbr, railroad.CtrlNbr, "Transportation");
        context.Set<Department>().Add(department);

        var craft = Craft.Create(
            parent.CtrlNbr,
            railroad.CtrlNbr,
            "Trainman",
            "Trainmen",
            1,
            autoMarkUp: false,
            approveAllMarkOffs: false,
            markOffHours: 0,
            markUpHours: 0,
            requiredRestHours: 0,
            maximumVacationDayTime: 0,
            unpaidMealPeriodMinutes: 0,
            hoursofService: false,
            processPayroll: false,
            showNotifications: false,
            vacationAssignmentType: 0,
            departmentCtrlNbr: department.CtrlNbr);
        context.Set<Craft>().Add(craft);

        var absenceCode = AbsenceCode.Create(
            railroad.CtrlNbr.Value,
            "CD",
            "Comp Day",
            isExcused: true,
            isCompensated: true,
            requiresApproval: true,
            isSystemOnly: false,
            isHolidayExempt: false,
            defaultAutoMarkUpHours: null,
            isActive: true);
        context.Set<AbsenceCode>().Add(absenceCode);
        await context.SaveChangesAsync(ct);

        var requestDate = new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc);
        var tieEntry = new DateTime(2026, 8, 30, 9, 0, 0, DateTimeKind.Utc);

        var tieA = AbsenceRequestWaitListRecord.CreateCompensableDay(
            employee.CtrlNbr,
            absenceCode.CtrlNbr,
            requestDate,
            tieEntry,
            craft.CtrlNbr,
            department.CtrlNbr);
        var tieB = AbsenceRequestWaitListRecord.CreateCompensableDay(
            employee.CtrlNbr,
            absenceCode.CtrlNbr,
            requestDate,
            tieEntry,
            craft.CtrlNbr,
            department.CtrlNbr);
        var later = AbsenceRequestWaitListRecord.CreateCompensableDay(
            employee.CtrlNbr,
            absenceCode.CtrlNbr,
            requestDate,
            tieEntry.AddMinutes(1),
            craft.CtrlNbr,
            department.CtrlNbr);

        var assigned = AbsenceRequestWaitListRecord.CreateCompensableDay(
            employee.CtrlNbr,
            absenceCode.CtrlNbr,
            requestDate,
            tieEntry.AddMinutes(2),
            craft.CtrlNbr,
            department.CtrlNbr);
        assigned.MarkAssigned(tieEntry.AddMinutes(10), "Assigned");

        var otherType = AbsenceRequestWaitListRecord.CreateVacationWeek(
            employee.CtrlNbr,
            absenceCode.CtrlNbr,
            requestDate,
            tieEntry.AddMinutes(3),
            craft.CtrlNbr,
            department.CtrlNbr);

        context.Set<AbsenceRequestWaitListRecord>().AddRange(tieB, later, assigned, otherType, tieA);
        await context.SaveChangesAsync(ct);

        var repository = new AbsenceRequestWaitListRecordRepository(context, _factory.CurrentUserService);
        var results = await repository.GetPendingByDateAsync(requestDate, AbsenceRequestWaitListType.CompensableDay, ct);

        Assert.Equal(3, results.Count);
        var expectedOrder = results
            .OrderBy(r => r.EntryUtc)
            .ThenBy(r => r.CtrlNbr.Value)
            .Select(r => r.CtrlNbr)
            .ToList();
        Assert.Equal(expectedOrder, results.Select(r => r.CtrlNbr).ToList());
        Assert.All(results, r => Assert.Null(r.AssignedAtUtc));
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