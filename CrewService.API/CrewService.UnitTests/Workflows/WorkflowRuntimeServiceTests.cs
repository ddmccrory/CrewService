using System.Text.Json;
using CrewService.Application.Modules.UserAccess;
using CrewService.Application.VacancyAssignment;
using CrewService.Application.UserAccess;
using CrewService.Application.Workflows;
using CrewService.Application.Workflows.Effects;
using CrewService.Domain.DomainEvents.Employees;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using CrewService.Persistance.Services;
using CrewService.Persistance.UnitOfWork;
using CrewService.Application.SeniorityOps;
using CrewService.Application.TenantConfig;
using CrewService.UnitTests.Fixtures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrewService.UnitTests.Workflows;

public sealed class WorkflowRuntimeServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ICurrentUserService _currentUser = new TestCurrentUserService();
    private readonly CrewServiceDbContext _crewContext;
    private readonly UserAccessDbContext _userContext;

    public WorkflowRuntimeServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var crewOptions = new DbContextOptionsBuilder<CrewServiceDbContext>()
            .UseSqlite(_connection)
            .Options;
        _crewContext = new CrewServiceDbContext(crewOptions, _currentUser, new TestFieldEncryptor());
        _crewContext.Database.EnsureCreated();

        var userOptions = new DbContextOptionsBuilder<UserAccessDbContext>()
            .UseSqlite(_connection)
            .Options;
        _userContext = new UserAccessDbContext(userOptions);
        _userContext.Database.EnsureCreated();

        SeedWorkflowReferenceData();
    }

    private void SeedWorkflowReferenceData()
    {
        if (!_crewContext.Set<WorkflowTriggerType>().Any())
        {
            _crewContext.Set<WorkflowTriggerType>().AddRange(
                WorkflowTriggerType.Create(WorkflowTriggerTypeCodes.EmployeeCreated, TriggerTypes.EmployeeCreated),
                WorkflowTriggerType.Create(WorkflowTriggerTypeCodes.SeniorityStatusChanged, TriggerTypes.SeniorityStatusChanged));
        }

        if (!_crewContext.Set<WorkflowEffectType>().Any())
        {
            _crewContext.Set<WorkflowEffectType>().AddRange(
                WorkflowEffectType.Create(WorkflowEffectTypeCodes.SendInvitation, WorkflowEffectTypes.SendInvitation),
                WorkflowEffectType.Create(WorkflowEffectTypeCodes.DoNothing, WorkflowEffectTypes.DoNothing),
                WorkflowEffectType.Create(WorkflowEffectTypeCodes.AddToRosterBoard, WorkflowEffectTypes.AddToRosterBoard),
                WorkflowEffectType.Create(WorkflowEffectTypeCodes.VacatePositionAndBulletinPosition, WorkflowEffectTypes.VacatePositionAndBulletinPosition));
        }

        if (!_crewContext.Set<WorkflowOperatorType>().Any())
        {
            _crewContext.Set<WorkflowOperatorType>().AddRange(
                WorkflowOperatorType.Create(WorkflowOperatorTypeCodes.EqualsOperator, "Equals"),
                WorkflowOperatorType.Create(WorkflowOperatorTypeCodes.NotEquals, "Does Not Equal"));
        }

        if (!_crewContext.Set<WorkflowMetadataFieldType>().Any())
        {
            _crewContext.Set<WorkflowMetadataFieldType>().AddRange(
                WorkflowMetadataFieldType.Create(WorkflowMetadataFieldTypeCodes.NewSeniorityState, "New Seniority Status"),
                WorkflowMetadataFieldType.Create(WorkflowMetadataFieldTypeCodes.DepartmentCtrlNbr, "Department CtrlNbr"),
                WorkflowMetadataFieldType.Create(WorkflowMetadataFieldTypeCodes.DepartmentName, "Department Name"),
                WorkflowMetadataFieldType.Create(WorkflowMetadataFieldTypeCodes.CraftCtrlNbr, "Craft CtrlNbr"),
                WorkflowMetadataFieldType.Create(WorkflowMetadataFieldTypeCodes.CraftName, "Craft Name"),
                WorkflowMetadataFieldType.Create(WorkflowMetadataFieldTypeCodes.SeniorityStateCtrlNbr, "Seniority Status CtrlNbr"),
                WorkflowMetadataFieldType.Create(WorkflowMetadataFieldTypeCodes.SeniorityStateName, "Seniority Status Name"));
        }

        _crewContext.SaveChanges();
    }

    [Fact]
    public async Task ExecuteEmployeeCreatedAsync_CreatesInvitation_UsesLatestPublishedVersionAndDefaultExpiration()
    {
        var ct = TestContext.Current.CancellationToken;
        var parentCtrlNbr = await SeedParentAsync(ct);
        var railroadCtrlNbr = await SeedRailroadAsync("RR-1", ct);
        var employee = await SeedEmployeeAsync(withPrimaryEmail: true, ct);
        var roleOlder = await SeedRoleAsync("Role Older", ct);
        var roleLatest = await SeedRoleAsync("Role Latest", ct);

        await SeedWorkflowVersionAsync(
            railroadCtrlNbr,
            templateEnabled: true,
            versionNumber: 1,
            status: WorkflowVersionStatus.Published,
            roleCtrlNbr: roleOlder,
            includeExpirationDays: true,
            expirationDays: 3,
            ct);

        await SeedWorkflowVersionAsync(
            railroadCtrlNbr,
            templateEnabled: true,
            versionNumber: 2,
            status: WorkflowVersionStatus.Published,
            roleCtrlNbr: roleLatest,
            includeExpirationDays: false,
            expirationDays: 0,
            ct);

        await SeedWorkflowVersionAsync(
            railroadCtrlNbr,
            templateEnabled: true,
            versionNumber: 3,
            status: WorkflowVersionStatus.Draft,
            roleCtrlNbr: roleOlder,
            includeExpirationDays: true,
            expirationDays: 1,
            ct);

        var runtime = BuildRuntimeService();
        var startedUtc = DateTime.UtcNow;
        await runtime.ExecuteEmployeeCreatedAsync(CreateEvent(employee, parentCtrlNbr, railroadCtrlNbr), ct);

        var invitation = Assert.Single(_crewContext.Invitations.ToList());
        Assert.Equal("role latest", invitation.Role.ToLowerInvariant());
        Assert.Equal("primary.employee@example.com", invitation.Email);

        var delta = invitation.ExpiresAt - startedUtc;
        Assert.InRange(delta.TotalDays, 6.8, 7.2);
    }

    [Fact]
    public async Task ExecuteEmployeeCreatedAsync_IgnoresDisabledTemplate_WhenResolvingPublishedWorkflow()
    {
        var ct = TestContext.Current.CancellationToken;
        var parentCtrlNbr = await SeedParentAsync(ct);
        var railroadCtrlNbr = await SeedRailroadAsync("RR-2", ct);
        var employee = await SeedEmployeeAsync(withPrimaryEmail: true, ct);
        var roleEnabled = await SeedRoleAsync("Role Enabled", ct);
        var roleDisabled = await SeedRoleAsync("Role Disabled", ct);

        await SeedWorkflowVersionAsync(
            railroadCtrlNbr,
            templateEnabled: false,
            versionNumber: 50,
            status: WorkflowVersionStatus.Published,
            roleCtrlNbr: roleDisabled,
            includeExpirationDays: true,
            expirationDays: 5,
            ct);

        await SeedWorkflowVersionAsync(
            railroadCtrlNbr,
            templateEnabled: true,
            versionNumber: 1,
            status: WorkflowVersionStatus.Published,
            roleCtrlNbr: roleEnabled,
            includeExpirationDays: true,
            expirationDays: 5,
            ct);

        var runtime = BuildRuntimeService();
        await runtime.ExecuteEmployeeCreatedAsync(CreateEvent(employee, parentCtrlNbr, railroadCtrlNbr), ct);

        var invitation = Assert.Single(_crewContext.Invitations.ToList());
        Assert.Equal("Role Enabled", invitation.Role);
    }

    [Fact]
    public async Task ExecuteEmployeeCreatedAsync_WithoutPrimaryEmail_ThrowsInvalidOperationException()
    {
        var ct = TestContext.Current.CancellationToken;
        var parentCtrlNbr = await SeedParentAsync(ct);
        var railroadCtrlNbr = await SeedRailroadAsync("RR-3", ct);
        var employee = await SeedEmployeeAsync(withPrimaryEmail: false, ct);
        var role = await SeedRoleAsync("Role Runtime", ct);

        await SeedWorkflowVersionAsync(
            railroadCtrlNbr,
            templateEnabled: true,
            versionNumber: 1,
            status: WorkflowVersionStatus.Published,
            roleCtrlNbr: role,
            includeExpirationDays: true,
            expirationDays: 5,
            ct);

        var runtime = BuildRuntimeService();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runtime.ExecuteEmployeeCreatedAsync(CreateEvent(employee, parentCtrlNbr, railroadCtrlNbr), ct));

        Assert.Contains("primary email", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(_crewContext.Invitations);
    }

    [Fact]
    public async Task ExecuteEmployeeCreatedAsync_WithoutPrimaryEmail_AndUsePrimaryEmailFalse_UsesTriggerEmail()
    {
        var ct = TestContext.Current.CancellationToken;
        var parentCtrlNbr = await SeedParentAsync(ct);
        var railroadCtrlNbr = await SeedRailroadAsync("RR-5", ct);
        var employee = await SeedEmployeeAsync(withPrimaryEmail: false, ct);
        var role = await SeedRoleAsync("Role Trigger Email", ct);

        await SeedWorkflowVersionAsync(
            railroadCtrlNbr,
            templateEnabled: true,
            versionNumber: 1,
            status: WorkflowVersionStatus.Published,
            roleCtrlNbr: role,
            includeExpirationDays: true,
            expirationDays: 5,
            ct,
            usePrimaryEmail: false);

        var runtime = BuildRuntimeService();
        await runtime.ExecuteEmployeeCreatedAsync(CreateEvent(employee, parentCtrlNbr, railroadCtrlNbr), ct);

        var invitation = Assert.Single(_crewContext.Invitations.ToList());
        Assert.Equal("event.payload@example.com", invitation.Email);
    }

    [Fact]
    public async Task ExecuteEmployeeCreatedAsync_WithoutPublishedWorkflow_ThrowsInvalidOperationException()
    {
        var ct = TestContext.Current.CancellationToken;
        var parentCtrlNbr = await SeedParentAsync(ct);
        var railroadCtrlNbr = await SeedRailroadAsync("RR-4", ct);
        var employee = await SeedEmployeeAsync(withPrimaryEmail: true, ct);

        var runtime = BuildRuntimeService();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runtime.ExecuteEmployeeCreatedAsync(CreateEvent(employee, parentCtrlNbr, railroadCtrlNbr), ct));

        Assert.Contains("No published workflow", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteEmployeeCreatedAsync_PostCommitDispatch_RunsWithoutAmbientTransaction()
    {
        var ct = TestContext.Current.CancellationToken;
        var parentCtrlNbr = await SeedParentAsync(ct);
        var railroadCtrlNbr = await SeedRailroadAsync("RR-POST", ct);
        var employee = await SeedEmployeeAsync(withPrimaryEmail: true, ct);
        var role = await SeedRoleAsync("Role Post Commit", ct);

        await SeedWorkflowVersionAsync(
            railroadCtrlNbr,
            templateEnabled: true,
            versionNumber: 1,
            status: WorkflowVersionStatus.Published,
            roleCtrlNbr: role,
            includeExpirationDays: true,
            expirationDays: 5,
            ct);

        var runtime = BuildRuntimeService(new AssertNoAmbientTransactionPostCommitDispatcher(() =>
            _crewContext.Database.CurrentTransaction is not null || _userContext.Database.CurrentTransaction is not null));

        await runtime.ExecuteEmployeeCreatedAsync(CreateEvent(employee, parentCtrlNbr, railroadCtrlNbr), ct);
    }

    [Fact]
    public async Task ExecuteSeniorityStatusChangedAsync_WhenNoPublishedWorkflow_DoesNotThrow()
    {
        var ct = TestContext.Current.CancellationToken;
        var railroad = await SeedRailroadWithParentAsync("RR-SEN-1", ct);

        var craft = Craft.Create(
            parentCtrlNbr: null,
            dynamicGroupCtrlNbr: railroad.CtrlNbr,
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
        await _crewContext.SaveChangesAsync(ct);

        var roster = Roster.Create(
            craftCtrlNbr: craft.CtrlNbr,
            workAreaGroupCtrlNbr: railroad.CtrlNbr,
            railroadPayrollDepartmentCtrlNbr: null,
            rosterName: "Roster",
            rosterPluralName: "Rosters",
            rosterNumber: 1);
        _crewContext.Rosters.Add(roster);

        var state = SeniorityState.Create("Active", StateType.Active, railroad.ParentCtrlNbr!.Value);
        _crewContext.Set<SeniorityState>().Add(state);
        await _crewContext.SaveChangesAsync(ct);

        var runtime = BuildRuntimeService();

        await runtime.ExecuteSeniorityStatusChangedAsync(
            employeeCtrlNbr: ControlNumber.Create(20001),
            newSeniorityStateCtrlNbr: state.CtrlNbr,
            rosterCtrlNbr: roster.CtrlNbr,
            ct);
    }

    private WorkflowRuntimeService BuildRuntimeService(IWorkflowPostCommitDispatcher? postCommitDispatcher = null)
    {
        var uowFactory = new OrchestrationUnitOfWorkFactory(
            _connection,
            _currentUser,
            new TestFieldEncryptor(),
            new WorkflowEffectExecutionGuard(),
            NullLoggerFactory.Instance);
        var invitationService = new InvitationAppService(
            uowFactory,
            _currentUser,
            new NoOpInvitationEmailService(),
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AppSettings:BaseUrl"] = "https://localhost"
                })
                .Build(),
            NullLogger<InvitationAppService>.Instance);

        var railroadResolver = new RailroadResolver();
        var workflowPostCommitDispatcher = postCommitDispatcher ?? new WorkflowPostCommitDispatcher(
            invitationService,
            new NoOpVacancyRepostService());
        var workflowEffectRunner = new WorkflowEffectRunner(
            new WorkflowEffectHandlerFactory([new SendInvitationWorkflowDatabaseEffect(invitationService)]),
            new WorkflowEffectExecutionTemplate(new WorkflowEffectExecutionGuard()));
        var triggerTemplate = new WorkflowTriggerExecutionTemplate(
            workflowEffectRunner,
            NullLogger<WorkflowTriggerExecutionTemplate>.Instance);

        return new WorkflowRuntimeService(
            uowFactory: uowFactory,
            workflowTriggerExecutionTemplate: triggerTemplate,
            workflowPostCommitDispatcher: workflowPostCommitDispatcher,
            railroadResolver: railroadResolver,
            logger: NullLogger<WorkflowRuntimeService>.Instance);
    }

    private sealed class AssertNoAmbientTransactionPostCommitDispatcher(Func<bool> hasAmbientTransaction) : IWorkflowPostCommitDispatcher
    {
        public Task DispatchAsync(IReadOnlyList<WorkflowEffectPostCommitWorkItem> workItems, CancellationToken ct = default)
        {
            if (hasAmbientTransaction())
                throw new InvalidOperationException("Post-commit dispatch ran while ambient transaction was still attached to DbContext.");

            return Task.CompletedTask;
        }
    }

    private sealed class NoOpVacancyRepostService : IVacancyRepostService
    {
        public Task RepostVacatedPositionAsync(
            ControlNumber staffablePositionCtrlNbr,
            ControlNumber? previousIncumbentCtrlNbr = null,
            CancellationToken ct = default)
            => Task.CompletedTask;

        public Task RepostBoardPositionIfUnderstaffedAsync(
            ControlNumber boardCtrlNbr,
            ControlNumber vacatedStaffablePositionCtrlNbr,
            ControlNumber? previousIncumbentCtrlNbr = null,
            CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<int> ReconcileUnbulletinedVacantPositionsAsync(CancellationToken ct = default)
            => Task.FromResult(0);
    }

    private async Task<(ControlNumber ParentCtrlNbr, ControlNumber CtrlNbr)> SeedRailroadWithParentAsync(string code, CancellationToken ct)
    {
        var parent = Parent.Create($"Parent {code}");
        _crewContext.Parents.Add(parent);
        await _crewContext.SaveChangesAsync(ct);

        var groupType = GroupType.Create("Railroad", "Railroad", true);
        _crewContext.GroupTypes.Add(groupType);
        await _crewContext.SaveChangesAsync(ct);

        var railroad = DynamicGroup.Create(
            groupType.CtrlNbr,
            $"Railroad {code}",
            parentGroupCtrlNbr: null,
            path: null,
            isWorkArea: false,
            code: code,
            parentCtrlNbr: parent.CtrlNbr,
            railroadCtrlNbr: null);

        _crewContext.DynamicGroups.Add(railroad);
        await _crewContext.SaveChangesAsync(ct);

        return (parent.CtrlNbr, railroad.CtrlNbr);
    }

    private async Task<ControlNumber> SeedRailroadAsync(string code, CancellationToken ct)
    {
        var (_, railroadCtrlNbr) = await SeedRailroadWithParentAsync(code, ct);
        return railroadCtrlNbr;
    }

    private async Task<Employee> SeedEmployeeAsync(bool withPrimaryEmail, CancellationToken ct)
    {
        var employmentStatus = EmploymentStatus.Create(
            clientCtrlNbr: ControlNumber.Create(500),
            statusCode: "ACT",
            statusName: "Active",
            statusNumber: 1,
            employmentCode: "A");

        _crewContext.EmploymentStatuses.Add(employmentStatus);

        var emailType = EmailAddressType.Create(ControlNumber.Create(500), "Personal", 1, emergencyType: false);
        _crewContext.EmailAddressTypes.Add(emailType);
        await _crewContext.SaveChangesAsync(ct);

        var employee = Employee.Create(
            clientCtrlNbr: ControlNumber.Create(500),
            railroadCtrlNbr: null,
            userId: Guid.NewGuid().ToString(),
            employeeNumber: "E001",
            ssn: Guid.NewGuid().ToString("N")[..9],
            gender: Gender.Male,
            race: Race.White,
            birthDate: new DateTime(1990, 1, 1),
            employmentDate: new DateTime(2020, 1, 1),
            employmentStatusCtrlNbr: employmentStatus.CtrlNbr,
            email: "event.payload@example.com",
            invitedByUserId: "seed-user",
            invitedByUserName: "Seed User");

        employee.AddEmailAddress("secondary.employee@example.com", emailType.CtrlNbr, isPrimary: false);
        if (withPrimaryEmail)
        {
            employee.AddEmailAddress("primary.employee@example.com", emailType.CtrlNbr, isPrimary: true);
        }

        _crewContext.Employees.Add(employee);
        await _crewContext.SaveChangesAsync(ct);
        return employee;
    }

    private async Task<ControlNumber> SeedRoleAsync(string roleName, CancellationToken ct)
    {
        var role = Role.Create(roleName, roleName, isSystem: false, level: 10);
        _crewContext.Roles.Add(role);
        await _crewContext.SaveChangesAsync(ct);
        return role.CtrlNbr;
    }

    private async Task SeedWorkflowVersionAsync(
        ControlNumber railroadCtrlNbr,
        bool templateEnabled,
        int versionNumber,
        string status,
        ControlNumber roleCtrlNbr,
        bool includeExpirationDays,
        int expirationDays,
        CancellationToken ct,
        bool usePrimaryEmail = true)
    {
        var triggerTypeCtrlNbr = await _crewContext.Set<WorkflowTriggerType>()
            .Where(t => t.Code == WorkflowTriggerTypeCodes.EmployeeCreated)
            .Select(t => t.CtrlNbr)
            .FirstAsync(ct);

        var effectTypeCtrlNbr = await _crewContext.Set<WorkflowEffectType>()
            .Where(t => t.Code == WorkflowEffectTypeCodes.SendInvitation)
            .Select(t => t.CtrlNbr)
            .FirstAsync(ct);

        var template = WorkflowTemplate.Create(
            railroadCtrlNbr,
            name: $"Invite Workflow {versionNumber}",
            triggerTypeCtrlNbr: triggerTypeCtrlNbr,
            isEnabled: templateEnabled);

        _crewContext.WorkflowTemplates.Add(template);
        await _crewContext.SaveChangesAsync(ct);

        var options = new Dictionary<string, string>
        {
            [WorkflowOptionKeys.RoleCtrlNbr] = roleCtrlNbr.Value.ToString(),
            [WorkflowOptionKeys.RailroadCtrlNbr] = railroadCtrlNbr.Value.ToString()
        };

        if (includeExpirationDays)
            options[WorkflowOptionKeys.ExpirationDays] = expirationDays.ToString();

        options[WorkflowOptionKeys.UsePrimaryEmail] = usePrimaryEmail ? "true" : "false";

        var definition = new WorkflowDefinition(
            TriggerTypeCtrlNbr: null,
            TriggerConditionGroupOperator: "ALL",
            TriggerConditions: [],
            [
                new WorkflowStepDefinition(
                    CtrlNbr: ControlNumber.Create(),
                    Order: 1,
                    Name: "Send Invitation",
                    IsEnabled: true,
                    FailurePolicy: WorkflowFailurePolicies.StopWorkflow,
                    ConditionGroupOperator: "ALL",
                    Conditions: [],
                    Effects:
                    [
                        new WorkflowEffectDefinition(
                            CtrlNbr: ControlNumber.Create(),
                            Order: 1,
                            IsEnabled: true,
                            EffectTypeCtrlNbr: effectTypeCtrlNbr,
                            Options: options)
                    ])
            ]);

        var definitionJson = JsonSerializer.Serialize(definition);
        var version = WorkflowVersion.Create(template.CtrlNbr, versionNumber, definitionJson, notes: "seed", status);
        _crewContext.WorkflowVersions.Add(version);
        await _crewContext.SaveChangesAsync(ct);
    }

    private static EmployeeCreatedDomainEvent CreateEvent(Employee employee, ControlNumber parentCtrlNbr, ControlNumber railroadCtrlNbr)
    {
        return new EmployeeCreatedDomainEvent(
            aggregateCtrlNbr: employee.CtrlNbr,
            clientCtrlNbr: parentCtrlNbr,
            railroadCtrlNbr: railroadCtrlNbr,
            email: "event.payload@example.com",
            invitedByUserId: "trigger-user-id",
            invitedByUserName: "Trigger User",
            parentName: "Parent");
    }

    private async Task<ControlNumber> SeedParentAsync(CancellationToken ct)
    {
        var parent = Parent.Create("Workflow Parent");
        _crewContext.Parents.Add(parent);
        await _crewContext.SaveChangesAsync(ct);
        return parent.CtrlNbr;
    }

    public void Dispose()
    {
        _crewContext.Dispose();
        _userContext.Dispose();
        _connection.Dispose();
    }

    private sealed class NoOpInvitationEmailService : IInvitationEmailService
    {
        public Task SendInvitationAsync(string toEmail, string role, string parentName, string acceptUrl, DateTime expiresUtc)
            => Task.CompletedTask;

        public Task SendReminderAsync(string toEmail, string role, string parentName, string acceptUrl, DateTime expiresUtc)
            => Task.CompletedTask;
    }

}