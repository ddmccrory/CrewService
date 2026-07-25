using CrewService.Application.RosterBoardOps;
using CrewService.Application.SeniorityOps;
using CrewService.Application.AbsenceVacancy;
using CrewService.Application.Assignments;
using CrewService.Application.Authorization;
using CrewService.Application.Boards;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Application.Bootstrap;
using CrewService.Application.Bulletins;
using CrewService.Application.BackgroundWorkers;
using CrewService.Application.ContactTypes;
using CrewService.Application.Crews;
using CrewService.Application.DailyOperations;
using CrewService.Application.Dispatching;
using CrewService.Application.ElectronicCalling;
using CrewService.Application.ElectronicCalling.Providers;
using CrewService.Application.Employees;
using CrewService.Application.Employment;
using CrewService.Application.FraCompliance;
using CrewService.Application.HolidayManagement;
using CrewService.Application.Absence;
using CrewService.Application.Notifications;
using CrewService.Application.Parents;
using CrewService.Application.Payroll;
using CrewService.Application.Policies;
using CrewService.Application.Qualifications;
using CrewService.Application.RailroadInfo;
using CrewService.Application.TenantConfig;
using CrewService.Application.Time;
using CrewService.Application.UserAccess;
using CrewService.Application.Qualifications.Evaluators;
using CrewService.Application.ReportingExports;
using CrewService.Application.ReportingExports.Formatters;
using CrewService.Application.ReportingExports.Renderers;
using CrewService.Application.VacancyAssignment;
using CrewService.Application.VacancyAssignment.Rules;
using CrewService.Application.WorkManagement;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Time abstraction (deterministic "now" + work-area timezone resolution)
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IWorkAreaClock, WorkAreaClock>();

        // B01 – FRA Compliance
        services.AddScoped<FraRecordSearchService>();
        services.AddScoped<FraDutyTourCalculator>();
        services.AddScoped<FraRestValidator>();
        services.AddScoped<FraConsecutiveDayTracker>();
        services.AddScoped<FraExcessServiceDetector>();
        services.AddScoped<FraMonthlyCapTracker>();
        services.AddScoped<CertificationEligibilityService>();
        services.AddScoped<CertificationExpirationService>();
        services.AddScoped<CertificationRevocationService>();
        services.AddScoped<FraCertificationConfigService>();
        services.AddScoped<FraComplianceService>();
        services.AddScoped<DrugAlcoholCertificationImpactHandler>();

        // B02 – Daily Operations
        services.AddSingleton<IDailyCallSheetScheduleSignal, DailyCallSheetScheduleSignal>();
        services.AddSingleton<IDailyCallSheetManualOverrideStore, DailyCallSheetManualOverrideStore>();
        services.AddScoped<IBackgroundJobNextRunResolver, BackgroundJobNextRunResolver>();
        services.AddSingleton<IWorkerHeartbeatRegistry, WorkerHeartbeatRegistry>();
        services.AddHostedService<BackgroundWorkers.Workers.DailyCallSheetWorker>();
        services.AddScoped<DailyOperationsService>();
        services.AddScoped<CallSheetIncumbentSyncService>();
        services.AddScoped<OnDutyPlacementService>();
        services.AddScoped<TieUpService>();
        services.AddScoped<DispatchingService>();

        // Seniority
        services.AddScoped<CraftAppService>();
        services.AddScoped<RosterAppService>();
        services.AddScoped<SeniorityStateAppService>();
        services.AddScoped<SeniorityStateVacancyConfigService>();
        services.AddScoped<SeniorityAppService>();

        // Employees
        services.AddScoped<EmployeeAppService>();
        services.AddScoped<PriorServiceCreditAppService>();

        // ContactTypes
        services.AddScoped<ContactTypesAppService>();

        // Employment
        services.AddScoped<EmploymentAppService>();

        // Payroll Tiers
        services.AddScoped<PayrollTierAppService>();

        // Parents
        services.AddScoped<ParentAppService>();

        // UserAccess
        services.AddScoped<UserAccessAppService>();
        services.AddScoped<InvitationAppService>();
        services.AddScoped<AuthAppService>();

        // Bootstrap
        services.AddScoped<BootstrapQueryService>();

        // Assignments
        services.AddScoped<AssignmentsService>();

        // Crews
        services.AddScoped<CrewsAppService>();

        // WorkManagement
        services.AddScoped<Application.WorkManagement.WorkManagementService>();

        // TenantConfig
        services.AddScoped<TenantConfigService>();
        services.AddSingleton<IRailroadResolver, RailroadResolver>();

        // RosterBoardOps
        services.AddScoped<RosterBoardAppService>();

        // Policies
        services.AddScoped<PoliciesService>();
        services.AddScoped<DepartmentReassignmentService>();
        services.AddScoped<SeniorityMoveCancellationPath>();
        services.AddScoped<IncumbentAssignmentPath>();

        // Authorization
        services.AddScoped<Application.Authorization.AuthorizationService>();
        services.AddScoped<IRequestActorContextPolicy, RequestActorContextPolicy>();

        // Notifications
        services.AddScoped<EmployeeNotificationService>();
        services.AddScoped<NotificationQueryService>();
        services.AddScoped<NotificationTypeConfigAppService>();
        services.AddScoped<NotificationTypeConfigResolver>();
        services.AddScoped<INotificationAcknowledgementEnforcer, NotificationAcknowledgementEnforcer>();
        services.AddScoped<INotificationDeliveryService, LoggingNotificationDeliveryService>();

        // Bulletins
        services.AddSingleton<IBulletinScheduleSignal, BulletinScheduleSignal>();
        services.AddScoped<BulletinsService>();
        services.AddScoped<VacancyAssignment.IVacancyRepostService, VacancyAssignment.VacancyRepostService>();
        services.AddHostedService<BackgroundWorkers.Workers.BulletinProcessingWorker>();
        // Seniority state change scheduling
        services.AddSingleton<ISeniorityStateChangeSignal, SeniorityStateChangeSignal>();
        services.AddHostedService<BackgroundWorkers.Workers.SeniorityStateChangeWorker>();

        // Seniority move scheduling and execution
        services.AddSingleton<ISeniorityMoveSignal, SeniorityMoveSignal>();
        services.AddScoped<SeniorityMoveExecutionService>();
        services.AddHostedService<BackgroundWorkers.Workers.SeniorityMoveWorker>();

        // Absence auto mark-off scheduling and execution
        services.AddSingleton<IAbsenceMarkOffSignal, AbsenceMarkOffSignal>();
        services.AddSingleton<IAutoMarkUpSignal, AutoMarkUpSignal>();
        services.AddHostedService<BackgroundWorkers.Workers.MarkOffRequestWorker>();
        services.AddHostedService<BackgroundWorkers.Workers.AutoMarkUpWorker>();

        // B03 – Mark-Off
        services.AddScoped<AutoMarkUpService>();
        services.AddScoped<CompensationBalanceService>();

        // B04 – Vacancy Assignment
        services.AddScoped<VacancyResolutionEngine>();
        services.AddScoped<IAssignmentStrategy, StandardAssignmentStrategy>();
        services.AddScoped<ISkipRule, WorkedCapRule>();
        services.AddScoped<ISkipRule, AlreadyOnDutyRule>();
        services.AddScoped<ISkipRule, AvailabilityRule>();
        services.AddScoped<ISkipRule, RestRule>();
        services.AddScoped<ISkipRule, MarkOffRule>();
        services.AddScoped<ISkipRule, QualificationRule>();
        services.AddScoped<ISkipRule, WeeklyHoursCapRule>();

        // B05 – Payroll Engine
        services.AddScoped<PayrollService>();
        services.AddScoped<EarningCodeResolver>();
        services.AddScoped<PayrollPeriodService>();

        // B06 – Electronic Calling
        services.AddScoped<CrewCallingService>();
        services.AddScoped<ICrewNotificationProvider, MockNotificationProvider>();
        services.AddScoped<CallSheetGenerationService>();

        // B08 – Roster Board Ops
        services.AddScoped<HangoutProcessingService>();
        services.AddScoped<NewHireService>();
        services.AddScoped<DailyStatusSnapshotService>();

        // B09 – Holiday services
        services.AddScoped<HolidayAutoGenerationService>();
        services.AddScoped<HolidayQualificationService>();
        services.AddScoped<HolidayPayrollGenerationService>();

        // B15 – Qualifications
        services.AddScoped<QualificationsService>();
        services.AddScoped<RequirementEvaluationService>();
        services.AddScoped<EmployeeEligibilityService>();
        services.AddScoped<QualificationReactiveService>();
        services.AddScoped<EmployeeReactiveService>();
        services.AddScoped<IRequirementEvaluator, ManualCompletionEvaluator>();
        services.AddScoped<IRequirementEvaluator, TimeFromEventEvaluator>();
        services.AddScoped<IRequirementEvaluator, ActivityCountEvaluator>();
        services.AddScoped<IRequirementEvaluator, TimeInRoleEvaluator>();
        services.AddScoped<IRequirementEvaluator, QualificationHeldEvaluator>();
        services.AddScoped<IRequirementEvaluator, FraCertificationHeldEvaluator>();

        // B10 – Reporting & Exports
        services.AddScoped<IPayrollExportFormatter, AdpExportFormatter>();
        services.AddScoped<IPayrollExportFormatter, UkgExportFormatter>();
        services.AddScoped<PayrollExportService>();
        services.AddScoped<PayrollImportService>();
        services.AddScoped<DailyReportGenerationService>();
        services.AddScoped<IReportRenderer, PlainTextReportRenderer>();
        services.AddScoped<IReportRenderer, PdfReportRenderer>();

        // Boards
        services.AddScoped<BoardCascadePolicyService>();
        services.AddScoped<RequiredPositionsStrategyAppService>();
        services.AddSingleton<IRequiredPositionsFormula, StaticFormula>();
        services.AddSingleton<IRequiredPositionsFormula, AnnualizedAverageFormula>();
        services.AddSingleton<IRequiredPositionsFormulaRegistry, RequiredPositionsFormulaRegistry>();

        // WorkManagement
        services.AddScoped<DepartmentService>();

        // AbsenceVacancy
        services.AddScoped<AbsenceRequestService>();
        services.AddScoped<IAbsenceApprovalPolicyResolver, DbAbsenceApprovalPolicyResolver>();

        // Absence
        services.AddScoped<AbsenceCodeService>();

        // Safety
        services.AddScoped<Application.Safety.SafetyService>();

        // RailroadInfo
        services.AddScoped<RailroadInfoService>();

        return services;
    }
}

