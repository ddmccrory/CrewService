using CrewService.Application.BackgroundWorkers;
using CrewService.Application.Authorization;
using CrewService.Application.DailyOperations;
using CrewService.Application.ElectronicCalling;
using CrewService.Application.Employees;
using CrewService.Application.Absence;
using CrewService.Application.Qualifications;
using CrewService.Application.Qualifications.Evaluators;
using CrewService.Application.RosterBoardOps;
using CrewService.Application.VacancyAssignment;
using CrewService.Domain.DomainEvents;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.HolidayManagement;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.Modules.Safety;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Modules.Workflows;
using CrewService.Infrastructure.Models.UserAccount;
using CrewService.Infrastructure.Outbox;
using CrewService.Persistance.Data;
using CrewService.Persistance.Encryption;
using CrewService.Persistance.Repositories;
using CrewService.Persistance.Services;
using CrewService.Persistance.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CrewService.Persistance;

public static class DependencyInjection
{
    /// <summary>
    /// Applies pending EF Core migrations for both SQLite databases
    /// (CrewService + UserAccess/Identity). Call once at startup before
    /// seeding or serving requests.
    /// </summary>
    public static async Task MigrateDatabasesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var configuration = sp.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("SQLiteConnection")
            ?? throw new InvalidOperationException("SQLiteConnection connection string not configured.");

        // Set SQLite pragmas on a startup connection before migrations run.
        await using var connection = new SqliteConnection(connectionString);
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000;";
            await cmd.ExecuteNonQueryAsync();
        }

        var crewDb = sp.GetRequiredService<CrewServiceDbContext>();
        await crewDb.Database.MigrateAsync();

        var userDb = sp.GetRequiredService<UserAccessDbContext>();
        await userDb.Database.MigrateAsync();
    }

    public static IServiceCollection AddPersistance(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IActorContextAccessor, ActorContextAccessor>();
        services.AddSingleton<IWorkflowEffectExecutionGuard, WorkflowEffectExecutionGuard>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IRequestActorContextResolver, RequestActorContextResolver>();
        services.AddSingleton<IFieldEncryptor, AesFieldEncryptor>();

        string? connectionString = configuration.GetConnectionString("SQLiteConnection")
            ?? throw new InvalidOperationException("SQLiteConnection connection string not configured.");

        // Request-scoped contexts used by non-orchestration CRUD flows.
        services.AddDbContext<UserAccessDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddDbContext<CrewServiceDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IOutboxDbContext>(sp => sp.GetRequiredService<CrewServiceDbContext>());

        services.AddIdentityCore<User>(options =>
        {
            options.Password.RequiredLength = 8;
            options.SignIn.RequireConfirmedEmail = true;
        }).AddRoles<IdentityRole>()
          .AddEntityFrameworkStores<UserAccessDbContext>();

        // Orchestration UoW Factory: creates fresh contexts/transaction per UoW instance.
        services.AddScoped<IOrchestrationUnitOfWorkFactory>(sp =>
            new OrchestrationUnitOfWorkFactory(
                connectionFactory: () => new SqliteConnection(connectionString),
                currentUserService: sp.GetRequiredService<ICurrentUserService>(),
                fieldEncryptor: sp.GetRequiredService<IFieldEncryptor>(),
                workflowEffectExecutionGuard: sp.GetRequiredService<IWorkflowEffectExecutionGuard>(),
                loggerFactory: sp.GetRequiredService<ILoggerFactory>(),
                dispatcher: sp.GetService<IOutboxDispatcher>(),
                reactor: sp.GetService<IDomainEventReactor>()));

        // Core Repositories
        services.AddScoped<IParentRepository, ParentRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IUserParentAssignmentRepository, UserParentAssignmentRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IWorkflowTemplateRepository, WorkflowTemplateRepository>();
        services.AddScoped<IWorkflowVersionRepository, WorkflowVersionRepository>();
        services.AddScoped<IWorkflowExecutionHistoryRepository, WorkflowExecutionHistoryRepository>();
        services.AddScoped<IWorkflowTriggerTypeRepository, WorkflowTriggerTypeRepository>();
        services.AddScoped<IWorkflowEffectTypeRepository, WorkflowEffectTypeRepository>();
        services.AddScoped<IWorkflowOperatorTypeRepository, WorkflowOperatorTypeRepository>();
        services.AddScoped<IWorkflowMetadataFieldTypeRepository, WorkflowMetadataFieldTypeRepository>();

        // ContactType Repositories
        services.AddScoped<IAddressTypeRepository, AddressTypeRepository>();
        services.AddScoped<IPhoneNumberTypeRepository, PhoneNumberTypeRepository>();
        services.AddScoped<IEmailAddressTypeRepository, EmailAddressTypeRepository>();

        // Employment Repositories
        services.AddScoped<IEmploymentStatusRepository, EmploymentStatusRepository>();
        services.AddScoped<IEmploymentStatusHistoryRepository, EmploymentStatusHistoryRepository>();
        services.AddScoped<IEmployeePriorServiceCreditRepository, EmployeePriorServiceCreditRepository>();

        // Seniority Repositories
        services.AddScoped<ICraftRepository, CraftRepository>();
        services.AddScoped<IRosterRepository, RosterRepository>();
        services.AddScoped<ISeniorityRepository, SeniorityRepository>();
        services.AddScoped<ISeniorityStateRepository, SeniorityStateRepository>();
        services.AddScoped<IPayrollTierRepository, PayrollTierRepository>();

        // Authorization Module Repositories
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IFeatureRepository, FeatureRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();

        // TenantConfig Module Repositories
        services.AddScoped<IGroupTypeRepository, GroupTypeRepository>();
        services.AddScoped<IDynamicGroupRepository, DynamicGroupRepository>();
        services.AddScoped<IGroupAttributeDefinitionRepository, GroupAttributeDefinitionRepository>();
        services.AddScoped<IGroupAttributeValueRepository, GroupAttributeValueRepository>();

        // WorkManagement Module Repositories
        services.AddScoped<IWorkInstanceRepository, WorkInstanceRepository>();
        services.AddScoped<ICraftRoleRepository, CraftRoleRepository>();
        services.AddScoped<ICraftRoleQualificationRepository, CraftRoleQualificationRepository>();
        services.AddScoped<IPositionSlotRepository, PositionSlotRepository>();
        services.AddScoped<ISlotRequirementRepository, SlotRequirementRepository>();
        services.AddScoped<IBoardSnapshotRepository, BoardSnapshotRepository>();
        services.AddScoped<IBoardSelectionDecisionRepository, BoardSelectionDecisionRepository>();

        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        // Staffing Module Repositories
        services.AddScoped<IStaffablePositionRepository, StaffablePositionRepository>();
        services.AddScoped<IPositionAssignmentRepository, PositionAssignmentRepository>();

        // Crews Module Repositories
        services.AddScoped<ICrewRepository, CrewRepository>();
        services.AddScoped<ICrewPositionRepository, CrewPositionRepository>();
        services.AddScoped<ICrewIncumbencyRepository, CrewIncumbencyRepository>();
        services.AddScoped<ICrewAssignmentRepository, CrewAssignmentRepository>();
        services.AddScoped<ICrewAttachmentInstanceRepository, CrewAttachmentInstanceRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        services.AddScoped<IAssignmentScheduleRepository, AssignmentScheduleRepository>();
        
        // Boards Module Repositories
        services.AddScoped<IBoardCascadePolicyRepository, BoardCascadePolicyRepository>();
        services.AddScoped<IRequiredPositionsStrategyRepository, RequiredPositionsStrategyRepository>();
        services.AddScoped<ICraftRequiredPositionsStrategyRepository, CraftRequiredPositionsStrategyRepository>();

        // Policies Module Repositories
        services.AddScoped<ICraftDisplacementPolicyRepository, CraftDisplacementPolicyRepository>();
        services.AddScoped<IDisplacementCaseRepository, DisplacementCaseRepository>();
        services.AddScoped<IDisplacementClaimRepository, DisplacementClaimRepository>();
        services.AddScoped<IBulletinPolicyRepository, BulletinPolicyRepository>();
        services.AddScoped<ICallSheetRuleRepository, CallSheetRuleRepository>();
        services.AddScoped<ICraftCallSheetRuleRepository, CraftCallSheetRuleRepository>();
        services.AddScoped<IAbsenceApprovalPolicyRepository, AbsenceApprovalPolicyRepository>();
        services.AddScoped<IDepartmentAbsenceRequestWindowPolicyRepository, DepartmentAbsenceRequestWindowPolicyRepository>();
        services.AddScoped<ICraftAbsenceWaitListPolicyRepository, CraftAbsenceWaitListPolicyRepository>();
        services.AddScoped<IDepartmentReassignmentRuleRepository, DepartmentReassignmentRuleRepository>();
        services.AddScoped<IAbsenceWaitListAllowancePolicyRepository, AbsenceWaitListAllowancePolicyRepository>();
        services.AddScoped<ISeniorityMovePolicyRepository, SeniorityMovePolicyRepository>();
        services.AddScoped<INoAccessPolicyRepository, NoAccessPolicyRepository>();
        services.AddScoped<ISeniorityMoveRepository, SeniorityMoveRepository>();

        // Bulletins Module Repositories
        services.AddScoped<IPositionVacancyRepository, PositionVacancyRepository>();
        services.AddScoped<IBulletinRepository, BulletinRepository>();
        services.AddScoped<IBulletinBidRepository, BulletinBidRepository>();
        services.AddScoped<IBulletinRuleRepository, BulletinRuleRepository>();
        services.AddScoped<IBulletinAccessAuditRepository, BulletinAccessAuditRepository>();

        // Notifications Module Repositories
        services.AddScoped<IEmployeeNotificationRepository, EmployeeNotificationRepository>();
        services.AddScoped<INotificationTypeConfigRepository, NotificationTypeConfigRepository>();

        // Dispatching Module Repositories
        services.AddScoped<IDispatchProjectionRepository, DispatchProjectionRepository>();
        services.AddScoped<IDispatchDecisionLogRepository, DispatchDecisionLogRepository>();
        services.AddScoped<IDispatchOverrideRepository, DispatchOverrideRepository>();
        services.AddScoped<IEmployeeBookingRepository, EmployeeBookingRepository>();

        // AbsenceVacancy Module Repositories
        services.AddScoped<IAbsenceRequestRepository, AbsenceRequestRepository>();
        services.AddScoped<IVacancyImpactRepository, VacancyImpactRepository>();
        services.AddScoped<IAbsenceRequestWaitListRecordRepository, AbsenceRequestWaitListRecordRepository>();
        services.AddScoped<IAbsenceRequestWaitListLinkRepository, AbsenceRequestWaitListLinkRepository>();

        // Payroll Module Repositories
        services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
        services.AddScoped<IPayrollRunRepository, PayrollRunRepository>();
        services.AddScoped<IPayrollRecordRepository, PayrollRecordRepository>();

        // Holiday / HolidayManagement Repositories
        services.AddScoped<IHolidayRepository, HolidayRepository>();
        services.AddScoped<IHolidayQualificationRuleRepository, HolidayQualificationRuleRepository>();
        services.AddScoped<IHolidayPayrollRecordRepository, HolidayPayrollRecordRepository>();
        services.AddScoped<IRailroadHolidaySelectionRepository, RailroadHolidaySelectionRepository>();

        // FRA Compliance Repositories (B01)
        services.AddScoped<IFraCertificationConfigRepository, FraCertificationConfigRepository>();
        services.AddScoped<IFraCertificationCheckConfigRepository, FraCertificationCheckConfigRepository>();
        services.AddScoped<IFraDutyTourRepository, FraDutyTourRepository>();
        services.AddScoped<IRegulatoryStandardRepository, RegulatoryStandardRepository>();
        services.AddScoped<IRegulatoryQualificationRepository, RegulatoryQualificationRepository>();
        services.AddScoped<IEmployeeCertificationReadRepository, EmployeeCertificationReadRepository>();
        services.AddScoped<IEmployeeCertificationRepository, EmployeeCertificationRepository>();
        services.AddScoped<ICertificationRevocationRepository, CertificationRevocationRepository>();
        services.AddScoped<IDrugAlcoholTestRepository, DrugAlcoholTestRepository>();
        services.AddScoped<IDrugAlcoholActionRepository, DrugAlcoholActionRepository>();
        services.AddScoped<IVoluntaryReferralRepository, VoluntaryReferralRepository>();

        // Daily Operations Repositories (B02)
        services.AddScoped<IShiftDefinitionRepository, ShiftDefinitionRepository>();
        services.AddScoped<IShiftInstanceRepository, ShiftInstanceRepository>();
        services.AddScoped<IOnDutyRecordRepository, OnDutyRecordRepository>();
        services.AddScoped<IOffDutyRecordRepository, OffDutyRecordRepository>();
        services.AddScoped<IDailyCallSheetSchedulerService, DailyCallSheetSchedulerService>();
        services.AddScoped<ICraftOperationsPolicyRepository, CraftOperationsPolicyRepository>();
        services.AddScoped<IAssignmentQueryService, AssignmentQueryService>();
        services.AddScoped<IEmployeeOnDutyQueryService, EmployeeOnDutyQueryService>();

        // Absence Repositories (B03)
        services.AddScoped<IAbsenceCodeRepository, AbsenceCodeRepository>();
        services.AddScoped<ICompensationBalanceRepository, CompensationBalanceRepository>();

        // Vacancy Assignment Repositories (B04)
        services.AddScoped<IVacancyResolutionRunRepository, VacancyResolutionRunRepository>();
        services.AddScoped<IOpenSlotProvider, OpenSlotProvider>();
        services.AddScoped<IBoardCandidateProvider, BoardCandidateProvider>();
        services.AddScoped<IBoardSnapshotSource, BoardSnapshotSource>();
        services.AddScoped<ISkipContextProvider, SkipContextProvider>();

        // Payroll Engine Repositories (B05)
        services.AddScoped<IEarningCodeRuleRepository, EarningCodeRuleRepository>();
        services.AddScoped<IPayRateRepository, PayRateRepository>();

        // Electronic Calling Repositories (B06)
        services.AddScoped<IVacancyCallRequestRepository, VacancyCallRequestRepository>();

        // Background Services Repositories (B07)
        services.AddScoped<IWorkerScheduleRepository, WorkerScheduleRepository>();
        services.AddScoped<IWorkerExecutionLogRepository, WorkerExecutionLogRepository>();
        services.AddScoped<IProcessingLockService, ProcessingLockService>();

        // Roster Board Repositories (B08)
        services.AddScoped<IRosterBoardRepository, RosterBoardRepository>();
        services.AddScoped<IDailyEmployeeStatusRepository, DailyEmployeeStatusRepository>();

        // Reporting & Exports Repositories (B10)
        services.AddScoped<IPayrollExportBatchRepository, PayrollExportBatchRepository>();
        services.AddScoped<IPayrollImportRecordRepository, PayrollImportRecordRepository>();

        // Railroad Information Repositories (B11)
        services.AddScoped<IRailroadInformationRepository, RailroadInformationRepository>();
        services.AddScoped<IRailroadInformationReadReceiptRepository, RailroadInformationReadReceiptRepository>();

        // Safety Repositories (B12)
        services.AddScoped<ISafetyObservationRepository, SafetyObservationRepository>();
        services.AddScoped<ISafetyObservationResolutionRepository, SafetyObservationResolutionRepository>();
        services.AddScoped<ISafetyCategoryRepository, SafetyCategoryRepository>();

        // Audit Log Query
        services.AddScoped<IAuditLogQuery, AuditLogQuery>();

        // Contact Repositories (Core)
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IPhoneNumberRepository, PhoneNumberRepository>();
        services.AddScoped<IEmailAddressRepository, EmailAddressRepository>();

        // Qualifications Repositories (B15)
        services.AddScoped<IQualificationTypeRepository, QualificationTypeRepository>();
        services.AddScoped<IQualificationRequirementRepository, QualificationRequirementRepository>();
        services.AddScoped<IEmployeeQualificationRepository, EmployeeQualificationRepository>();

        // Qualifications Read Providers (B15)
        services.AddScoped<IOnDutyRecordCounter, OnDutyRecordCounter>();
        services.AddScoped<ICraftMembershipDateProvider, CraftMembershipDateProvider>();
        services.AddScoped<IFraCertificationChecker, FraCertificationChecker>();
        services.AddScoped<IRegulatoryQualificationCatalog, RegulatoryQualificationCatalog>();

        return services;
    }
}

