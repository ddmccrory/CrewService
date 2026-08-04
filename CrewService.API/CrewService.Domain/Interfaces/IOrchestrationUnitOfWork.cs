using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.HolidayManagement;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.Modules.Safety;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Modules.Workflows;

namespace CrewService.Domain.Interfaces;

/// <summary>
/// Short-lived orchestration Unit of Work for atomic multi-context flows.
/// Creates a single shared DbConnection + DbTransaction and instantiates contexts as needed.
/// Provides access to all repositories for various orchestration scenarios.
/// </summary>
public interface IOrchestrationUnitOfWork : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Correlation ID for logging and event tracing across the orchestration.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Orchestration ID for grouping related domain events.
    /// </summary>
    string OrchestrationId { get; }

    // ──────────────────────────────────────────────────────────────────
    // Identity User Profile
    // ──────────────────────────────────────────────────────────────────
    Task UpdateUserProfileAsync(
        string userId,
        string firstName, string? middleName, string lastName,
        string fullName, string fullNameLnf,
        CancellationToken cancellationToken = default);

    Task UpdateUserProfileAsync(
        string userId,
        string firstName, string? middleName, string lastName,
        string fullName, string fullNameLnf,
        string employeeNumber,
        CancellationToken cancellationToken = default);

    // ──────────────────────────────────────────────────────────────────
    // Core Employee Orchestration
    // ──────────────────────────────────────────────────────────────────
    IEmployeeRepository Employees { get; }
    IEmailAddressRepository EmailAddresses { get; }
    IParentRepository Parents { get; }

    // ──────────────────────────────────────────────────────────────────
    // UserAccess
    // ──────────────────────────────────────────────────────────────────
    IUserParentAssignmentRepository UserParentAssignments { get; }
    IInvitationRepository Invitations { get; }

    // ──────────────────────────────────────────────────────────────────
    // Workflows
    // ──────────────────────────────────────────────────────────────────
    IWorkflowTemplateRepository WorkflowTemplates =>
        throw new NotSupportedException("WorkflowTemplates repository is not implemented for this unit of work.");
    IWorkflowVersionRepository WorkflowVersions =>
        throw new NotSupportedException("WorkflowVersions repository is not implemented for this unit of work.");
    IWorkflowExecutionHistoryRepository WorkflowExecutionHistories =>
        throw new NotSupportedException("WorkflowExecutionHistories repository is not implemented for this unit of work.");
    IWorkflowTriggerTypeRepository WorkflowTriggerTypes =>
        throw new NotSupportedException("WorkflowTriggerTypes repository is not implemented for this unit of work.");
    IWorkflowEffectTypeRepository WorkflowEffectTypes =>
        throw new NotSupportedException("WorkflowEffectTypes repository is not implemented for this unit of work.");
    IWorkflowOperatorTypeRepository WorkflowOperatorTypes =>
        throw new NotSupportedException("WorkflowOperatorTypes repository is not implemented for this unit of work.");
    IWorkflowMetadataFieldTypeRepository WorkflowMetadataFieldTypes =>
        throw new NotSupportedException("WorkflowMetadataFieldTypes repository is not implemented for this unit of work.");

    // ──────────────────────────────────────────────────────────────────
    // Payroll Tiers
    // ──────────────────────────────────────────────────────────────────
    IPayrollTierRepository PayrollTiers { get; }

    // ──────────────────────────────────────────────────────────────────
    // ContactTypes
    // ──────────────────────────────────────────────────────────────────
    IAddressTypeRepository AddressTypes { get; }
    IPhoneNumberTypeRepository PhoneNumberTypes { get; }
    IEmailAddressTypeRepository EmailAddressTypes { get; }

    // ──────────────────────────────────────────────────────────────────
    // Employment
    // ──────────────────────────────────────────────────────────────────
    IEmploymentStatusRepository EmploymentStatuses { get; }
    IEmploymentStatusHistoryRepository EmploymentStatusHistory { get; }
    IEmployeePriorServiceCreditRepository EmployeePriorServiceCredits { get; }

    // ──────────────────────────────────────────────────────────────────
    // Seniority
    // ──────────────────────────────────────────────────────────────────
    ICraftRepository Crafts { get; }
    IRosterRepository Rosters { get; }
    ISeniorityRepository Seniority { get; }
    ISeniorityStateRepository SeniorityStates { get; }
    IPendingSeniorityStateChangeRepository PendingSeniorityStateChanges { get; }

    // ──────────────────────────────────────────────────────────────────
    // TenantConfig
    // ──────────────────────────────────────────────────────────────────
    IGroupTypeRepository GroupTypes { get; }
    IDynamicGroupRepository DynamicGroups { get; }
    IGroupAttributeDefinitionRepository AttributeDefinitions { get; }
    IGroupAttributeValueRepository AttributeValues { get; }

    // ──────────────────────────────────────────────────────────────────
    // ──────────────────────────────────────────────────────────────────
    // Staffing
    // ──────────────────────────────────────────────────────────────────
    IStaffablePositionRepository StaffablePositions { get; }
    IPositionAssignmentRepository PositionAssignments { get; }

    // ──────────────────────────────────────────────────────────────────
    // Boards
    // ──────────────────────────────────────────────────────────────────
    IBoardCascadePolicyRepository BoardCascadePolicies { get; }
    IRosterBoardRepository RosterBoards { get; }
    IRequiredPositionsStrategyRepository RequiredPositionsStrategies { get; }
    ICraftRequiredPositionsStrategyRepository CraftRequiredPositionsStrategies { get; }

    // ──────────────────────────────────────────────────────────────────
    // Crews
    // ──────────────────────────────────────────────────────────────────
    ICrewRepository Crews { get; }
    ICrewPositionRepository CrewPositions { get; }
    ICrewIncumbencyRepository CrewIncumbencies { get; }
    ICrewAssignmentRepository CrewAssignments { get; }
    ICrewAttachmentInstanceRepository CrewAttachmentInstances { get; }

    // ──────────────────────────────────────────────────────────────────
    // Assignments
    // ──────────────────────────────────────────────────────────────────
    IAssignmentRepository Assignments { get; }
    IAssignmentScheduleRepository AssignmentSchedules { get; }

    // ──────────────────────────────────────────────────────────────────
    // WorkManagement
    // ──────────────────────────────────────────────────────────────────
    IDepartmentRepository Departments { get; }
    ICraftRoleRepository CraftRoles { get; }
    ICraftRoleQualificationRepository CraftRoleQualifications { get; }
    IWorkInstanceRepository WorkInstances { get; }
    IPositionSlotRepository PositionSlots { get; }
    ISlotRequirementRepository SlotRequirements { get; }
    IShiftDefinitionRepository ShiftDefinitions { get; }
    IShiftInstanceRepository ShiftInstances { get; }
    IBoardSnapshotRepository BoardSnapshots =>
        throw new NotSupportedException("BoardSnapshots repository is not implemented for this unit of work.");
    IBoardSelectionDecisionRepository BoardSelectionDecisions =>
        throw new NotSupportedException("BoardSelectionDecisions repository is not implemented for this unit of work.");
    IOnDutyRecordRepository OnDutyRecords { get; }
    IOffDutyRecordRepository OffDutyRecords { get; }
    // ────────────────────────────────────────────────────
    // Policies
    // ────────────────────────────────────────────────────
    ICraftOperationsPolicyRepository CraftOperationsPolicies { get; }
    ICraftDisplacementPolicyRepository CraftDisplacementPolicies { get; }
    IDisplacementCaseRepository DisplacementCases { get; }
    IDisplacementClaimRepository DisplacementClaims { get; }
    IBulletinPolicyRepository BulletinPolicies { get; }
    ICallSheetRuleRepository CallSheetRules { get; }
    ICraftCallSheetRuleRepository CraftCallSheetRules { get; }
    IAbsenceApprovalPolicyRepository AbsenceApprovalPolicies { get; }
    IDepartmentReassignmentRuleRepository DepartmentReassignmentRules { get; }
    ISeniorityMovePolicyRepository SeniorityMovePolicies { get; }
    INoAccessPolicyRepository NoAccessPolicies =>
        throw new NotSupportedException("NoAccessPolicies repository is not implemented for this unit of work.");
    ISeniorityMoveRepository SeniorityMoves { get; }
    // ────────────────────────────────────────────────────
    // Dispatching
    // ────────────────────────────────────────────────────
    IDispatchProjectionRepository DispatchProjections { get; }
    IDispatchDecisionLogRepository DispatchDecisionLogs { get; }
    IDispatchOverrideRepository DispatchOverrides { get; }
    IEmployeeBookingRepository EmployeeBookings { get; }

    // ──────────────────────────────────────────────────────────────────
    // FRA Compliance
    // ──────────────────────────────────────────────────────────────────
    IEmployeeCertificationRepository EmployeeCertifications { get; }
    IEmployeeCertificationReadRepository EmployeeCertificationReads { get; }
    IFraCertificationConfigRepository FraCertificationConfigs { get; }
    IFraCertificationCheckConfigRepository FraCertificationCheckConfigs { get; }
    IFraDutyTourRepository FraDutyTours { get; }
    IRegulatoryStandardRepository RegulatoryStandards { get; }
    IRegulatoryQualificationRepository RegulatoryQualifications { get; }
    ICertificationRevocationRepository CertificationRevocations { get; }
    IDrugAlcoholTestRepository DrugAlcoholTests { get; }
    IDrugAlcoholActionRepository DrugAlcoholActions { get; }
    IVoluntaryReferralRepository VoluntaryReferrals { get; }

    // ──────────────────────────────────────────────────────────────────
    // Qualifications
    // ──────────────────────────────────────────────────────────────────
    IQualificationTypeRepository QualificationTypes { get; }
    IQualificationRequirementRepository QualificationRequirements { get; }
    IEmployeeQualificationRepository EmployeeQualifications { get; }
    IEmployeeQualificationSuspensionRepository QualificationSuspensions { get; }

    // ──────────────────────────────────────────────────────────────────
    // Absence & Vacancy
    // ──────────────────────────────────────────────────────────────────
    IAbsenceRequestRepository AbsenceRequests { get; }
    IAbsenceRequestWaitListRecordRepository AbsenceRequestWaitListRecords =>
        throw new NotSupportedException("AbsenceRequestWaitListRecords repository is not implemented for this unit of work.");
    IAbsenceRequestWaitListLinkRepository AbsenceRequestWaitListLinks =>
        throw new NotSupportedException("AbsenceRequestWaitListLinks repository is not implemented for this unit of work.");
    IVacancyImpactRepository VacancyImpacts { get; }

    // ──────────────────────────────────────────────────────────────────
    // Safety
    // ──────────────────────────────────────────────────────────────────
    ISafetyObservationRepository SafetyObservations { get; }
    ISafetyObservationResolutionRepository SafetyResolutions { get; }
    ISafetyCategoryRepository SafetyCategories { get; }

    // ──────────────────────────────────────────────────────────────────
    // Railroad Info
    // ──────────────────────────────────────────────────────────────────
    IRailroadInformationRepository RailroadInformation { get; }
    IRailroadInformationReadReceiptRepository RailroadInformationReadReceipts { get; }
    // ────────────────────────────────────────────────────
    // Payroll
    // ────────────────────────────────────────────────────
    ITimeEntryRepository TimeEntries { get; }
    IPayrollRunRepository PayrollRuns { get; }
    IPayrollRecordRepository PayrollRecords { get; }
    IPayrollExportBatchRepository PayrollExportBatches { get; }
    IPayrollImportRecordRepository PayrollImportRecords { get; }
    IHolidayRepository Holidays { get; }
    IHolidayQualificationRuleRepository HolidayQualificationRules { get; }
    IHolidayPayrollRecordRepository HolidayPayrollRecords { get; }
    IEarningCodeRuleRepository EarningCodeRules { get; }
    IPayRateRepository PayRates { get; }
    IRailroadHolidaySelectionRepository RailroadHolidaySelections { get; }
    // ────────────────────────────────────────────────────
    // Authorization
    // ────────────────────────────────────────────────────
    IRoleRepository Roles { get; }
    IFeatureRepository Features { get; }
    IPermissionRepository Permissions { get; }
    // ────────────────────────────────────────────────────
    // Bulletins
    // ────────────────────────────────────────────────────
    IPositionVacancyRepository PositionVacancies { get; }
    IBulletinRepository Bulletins { get; }
    IBulletinBidRepository BulletinBids { get; }
    IBulletinRuleRepository BulletinRules { get; }
    IBulletinAccessAuditRepository BulletinAccessAudits =>
        throw new NotSupportedException("BulletinAccessAudits repository is not implemented for this unit of work.");

    // ────────────────────────────────────────────────────
    // Notifications
    // ────────────────────────────────────────────────────
    IEmployeeNotificationRepository EmployeeNotifications { get; }
    INotificationTypeConfigRepository NotificationTypeConfigs { get; }
    IPositionChangeRecordRepository PositionChangeRecords =>
        throw new NotSupportedException("PositionChangeRecords repository is not implemented for this unit of work.");

    /// <summary>
    /// Collects domain events from tracked entities, persists OutboxMessage rows,
    /// saves all changes, and commits the transaction atomically.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all entity changes and outbox rows within the existing transaction
    /// without committing it. Use when the caller controls the transaction lifetime.
    /// </summary>
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction. Call on error before disposing.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
