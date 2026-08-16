using CrewService.Application.Crews;
using CrewService.Application.Notifications;
using CrewService.Application.Policies;
using CrewService.Application.Qualifications;
using CrewService.Application.RosterBoardOps;
using CrewService.Application.TenantConfig;
using CrewService.Application.VacancyAssignment;
using CrewService.Application.Workflows.Effects;
using CrewService.Application.Bulletins;
using CrewService.Application.BackgroundWorkers;
using CrewService.Application.Boards;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.UnitOfWork;
using CrewService.UnitTests.Fixtures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrewService.UnitTests.Workflows;

public sealed class SeniorityWorkflowAssignmentPathInvariantTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CrewServiceDbContext _crewContext;
    private readonly UserAccessDbContext _userContext;
    private readonly IOrchestrationUnitOfWorkFactory _uowFactory;
    private readonly SeniorityWorkflowAssignmentPath _sut;

    public SeniorityWorkflowAssignmentPathInvariantTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var currentUser = new TestCurrentUserService();
        var encryptor = new TestFieldEncryptor();

        var crewOptions = new DbContextOptionsBuilder<CrewServiceDbContext>()
            .UseSqlite(_connection)
            .Options;
        _crewContext = new CrewServiceDbContext(crewOptions, currentUser, encryptor);
        _crewContext.Database.EnsureCreated();

        var userOptions = new DbContextOptionsBuilder<UserAccessDbContext>()
            .UseSqlite(_connection)
            .Options;
        _userContext = new UserAccessDbContext(userOptions);
        _userContext.Database.EnsureCreated();

        _uowFactory = new OrchestrationUnitOfWorkFactory(
            _connection,
            currentUser,
            encryptor,
            NullLoggerFactory.Instance);

        var railroadResolver = new RailroadResolver();
        var notifications = new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            railroadResolver,
            new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance));
        var vacancySync = TestCallSheetVacancyProjectionSyncFactory.Create(_uowFactory);
        var requirementEvaluation = new RequirementEvaluationService(_uowFactory, []);
        var repost = new VacancyRepostService(
            _uowFactory,
            new BulletinsService(
                _uowFactory,
                NullLogger<BulletinsService>.Instance,
                new BulletinScheduleSignal(),
                notifications,
                new EmployeeEligibilityService(_uowFactory),
                vacancySync),
            NullLogger<VacancyRepostService>.Instance);

        var crews = new CrewsAppService(
            _uowFactory,
            repost,
            vacancySync,
            NullLogger<CrewsAppService>.Instance);

        var rosterBoards = new RosterBoardAppService(
            _uowFactory,
            requirementEvaluation,
            new RequiredPositionsFormulaRegistry([new StaticFormula(), new AnnualizedAverageFormula()]),
            repost,
            notifications,
            vacancySync);

        _sut = new SeniorityWorkflowAssignmentPath(crews, rosterBoards);
    }

    [Fact]
    public async Task VacateEmployeeAssignmentsAsync_BoardAssignmentWithoutSource_ThrowsInvariantException()
    {
        var ct = TestContext.Current.CancellationToken;
        var employeeCtrlNbr = await SeedEmployeeWithBoardAssignmentMissingSourceAsync(ct);

        await using var uow = await _uowFactory.CreateAsync(cancellationToken: ct);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.VacateEmployeeAssignmentsAsync(uow, employeeCtrlNbr, ct));

        Assert.Contains("Board assignment source is missing", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ControlNumber> SeedEmployeeWithBoardAssignmentMissingSourceAsync(CancellationToken ct)
    {
        var parent = Parent.Create("Parent");
        _crewContext.Parents.Add(parent);
        await _crewContext.SaveChangesAsync(ct);

        var groupType = GroupType.Create("Railroad", "Railroad", true);
        _crewContext.GroupTypes.Add(groupType);
        await _crewContext.SaveChangesAsync(ct);

        var workArea = DynamicGroup.Create(groupType.CtrlNbr, "WorkArea", null, null, true, "WA", parentCtrlNbr: parent.CtrlNbr);
        _crewContext.DynamicGroups.Add(workArea);

        var craft = Craft.Create(
            parentCtrlNbr: null,
            dynamicGroupCtrlNbr: workArea.CtrlNbr,
            craftName: "Engineer",
            craftPluralName: "Engineers",
            craftNumber: 1,
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
            vacationAssignmentType: 0);
        _crewContext.Crafts.Add(craft);

        var roster = Roster.Create(craft.CtrlNbr, workArea.CtrlNbr, null, "Roster", "Rosters", 1);
        _crewContext.Rosters.Add(roster);

        var board = RosterBoard.Create(craft.CtrlNbr, roster.CtrlNbr, "Extra", BoardType.ExtraBoard, RotationType.StandardRotation, true);
        _crewContext.RosterBoards.Add(board);

        var employmentStatus = EmploymentStatus.Create(workArea.CtrlNbr, "ACT", "Active", 1, "A");
        _crewContext.EmploymentStatuses.Add(employmentStatus);
        await _crewContext.SaveChangesAsync(ct);

        var employee = Employee.Create(
            workArea.CtrlNbr,
            userId: "u1",
            employeeNumber: "E1",
            ssn: "123456789",
            gender: Gender.Male,
            race: Race.White,
            birthDate: new DateTime(1990, 1, 1),
            employmentDate: DateTime.UtcNow.AddYears(-1),
            employmentStatusCtrlNbr: employmentStatus.CtrlNbr,
            email: "e1@example.com",
            invitedByUserId: "admin",
            invitedByUserName: "Admin");
        _crewContext.Employees.Add(employee);
        await _crewContext.SaveChangesAsync(ct);

        var staffablePosition = StaffablePosition.Create(StaffablePositionType.Board);
        _crewContext.StaffablePositions.Add(staffablePosition);
        await _crewContext.SaveChangesAsync(ct);

        board.AddPosition(employee.CtrlNbr, 1, staffablePosition.CtrlNbr);
        _crewContext.RosterBoards.Update(board);

        var assignment = PositionAssignment.Create(
            staffablePosition.CtrlNbr,
            employee.CtrlNbr,
            PositionAssignmentType.Board,
            assignmentSourceCtrlNbr: null,
            assignedDateUtc: DateTime.UtcNow);
        _crewContext.PositionAssignments.Add(assignment);

        await _crewContext.SaveChangesAsync(ct);
        return employee.CtrlNbr;
    }

    public void Dispose()
    {
        _crewContext.Dispose();
        _userContext.Dispose();
        _connection.Dispose();
    }
}