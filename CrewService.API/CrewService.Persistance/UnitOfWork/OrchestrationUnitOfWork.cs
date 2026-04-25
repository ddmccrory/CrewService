using System.Data.Common;
using CrewService.Domain.DomainEvents;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Outbox;
using CrewService.Domain.Primitives;
using CrewService.Persistance.Data;
using CrewService.Persistance.Modules.Crews;
using CrewService.Persistance.Modules.Boards;
using CrewService.Persistance.Modules.Staffing;
using CrewService.Persistance.Modules.DailyOperations;
using CrewService.Persistance.Modules.TenantConfig;
using CrewService.Persistance.Modules.WorkManagement;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Persistance.Modules.FraCompliance;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Persistance.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Safety;
using CrewService.Persistance.Modules.Safety;
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Persistance.Modules.RailroadInfo;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.HolidayManagement;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Persistance.Modules.Authorization;
using CrewService.Persistance.Modules.Bulletins;
using CrewService.Persistance.Modules.Dispatching;
using CrewService.Persistance.Modules.Payroll;
using CrewService.Persistance.Modules.Policies;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CrewService.Persistance.UnitOfWork;

/// <summary>
/// Short-lived orchestration UoW that creates a single shared DbConnection + DbTransaction
/// and instantiates one or both DbContexts using the same connection.
/// Provides access to all repositories for various orchestration scenarios.
/// </summary>
internal sealed class OrchestrationUnitOfWork : IOrchestrationUnitOfWork
{
    private readonly DbConnection _connection;
    private readonly DbTransaction _transaction;
    private readonly CrewServiceDbContext _crewContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<OrchestrationUnitOfWork> _logger;
    private readonly string _idempotencyKey;
    private readonly IOutboxDispatcher? _dispatcher;

    private bool _committed;
    private bool _disposed;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Core Employee
    // ──────────────────────────────────────────────────────────────────
    private IEmployeeRepository? _employees;
    private IParentRepository? _parents;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: ContactTypes
    // ──────────────────────────────────────────────────────────────────
    private IAddressTypeRepository? _addressTypes;
    private IPhoneNumberTypeRepository? _phoneNumberTypes;
    private IEmailAddressTypeRepository? _emailAddressTypes;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Employment
    // ──────────────────────────────────────────────────────────────────
    private IEmploymentStatusRepository? _employmentStatuses;
    private IEmploymentStatusHistoryRepository? _employmentStatusHistory;
    private IEmployeePriorServiceCreditRepository? _employeePriorServiceCredits;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Seniority
    // ──────────────────────────────────────────────────────────────────
    private ICraftRepository? _crafts;
    private IRosterRepository? _rosters;
    private ISeniorityRepository? _seniority;
    private ISeniorityStateRepository? _seniorityStates;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: TenantConfig
    // ──────────────────────────────────────────────────────────────────
    private IGroupTypeRepository? _groupTypes;
    private IDynamicGroupRepository? _dynamicGroups;
    private IGroupAttributeDefinitionRepository? _attributeDefinitions;
    private IGroupAttributeValueRepository? _attributeValues;

    // ──────────────────────────────────────────────────────────────────
    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Staffing
    // ──────────────────────────────────────────────────────────────────
    private IStaffablePositionRepository? _staffablePositions;
    private IPositionAssignmentRepository? _positionAssignments;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Boards
    // ──────────────────────────────────────────────────────────────────
    private IBoardCascadePolicyRepository? _boardCascadePolicies;
    private IRosterBoardRepository? _rosterBoards;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Crews
    // ──────────────────────────────────────────────────────────────────
    private ICrewRepository? _crews;
    private ICrewPositionRepository? _crewPositions;
    private ICrewIncumbencyRepository? _crewIncumbencies;
    private ICrewAssignmentRepository? _crewAssignments;
    private ICrewAttachmentInstanceRepository? _crewAttachmentInstances;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Assignments
    // ──────────────────────────────────────────────────────────────────
    private IAssignmentRepository? _assignments;
    private IAssignmentScheduleRepository? _assignmentSchedules;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: WorkManagement
    // ──────────────────────────────────────────────────────────────────
    private IDepartmentRepository? _departments;
    private ICraftRoleRepository? _craftRoles;
    private ICraftRoleQualificationRepository? _craftRoleQualifications;
    private IWorkInstanceRepository? _workInstances;
    private IPositionSlotRepository? _positionSlots;
    private ISlotRequirementRepository? _slotRequirements;
    private IShiftDefinitionRepository? _shiftDefinitions;
    private IShiftInstanceRepository? _shiftInstances;
    private IOnDutyRecordRepository? _onDutyRecords;
    private IOffDutyRecordRepository? _offDutyRecords;
    private ICraftOperationsPolicyRepository? _craftOperationsPolicies;
    private ICraftDisplacementPolicyRepository? _craftDisplacementPolicies;
    private IDisplacementCaseRepository? _displacementCases;
    private IDisplacementClaimRepository? _displacementClaims;
    private IBulletinPolicyRepository? _bulletinPolicies;
    private ISeniorityMovePolicyRepository? _seniorityMovePolicies;
    private ISeniorityMoveRepository? _seniorityMoves;
    private IDispatchProjectionRepository? _dispatchProjections;
    private IDispatchDecisionLogRepository? _dispatchDecisionLogs;
    private IDispatchOverrideRepository? _dispatchOverrides;
    private IEmployeeBookingRepository? _employeeBookings;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Qualifications
    // ──────────────────────────────────────────────────────────────────
    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: FRA Compliance
    // ──────────────────────────────────────────────────────────────────
    private IEmployeeCertificationRepository? _employeeCertifications;
    private IEmployeeCertificationReadRepository? _employeeCertificationReads;
    private IFraCertificationConfigRepository? _fraCertificationConfigs;
    private IFraCertificationCheckConfigRepository? _fraCertificationCheckConfigs;
    private IFraDutyTourRepository? _fraDutyTours;
    private IRegulatoryStandardRepository? _regulatoryStandards;
    private IRegulatoryQualificationRepository? _regulatoryQualifications;
    private ICertificationRevocationRepository? _certificationRevocations;
    private IDrugAlcoholTestRepository? _drugAlcoholTests;
    private IDrugAlcoholActionRepository? _drugAlcoholActions;
    private IVoluntaryReferralRepository? _voluntaryReferrals;

    private IQualificationTypeRepository? _qualificationTypes;
    private IQualificationRequirementRepository? _qualificationRequirements;
    private IEmployeeQualificationRepository? _employeeQualifications;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Absence & Vacancy
    // ──────────────────────────────────────────────────────────────────
    private IAbsenceRequestRepository? _absenceRequests;
    private IVacancyImpactRepository? _vacancyImpacts;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Safety
    // ──────────────────────────────────────────────────────────────────
    private ISafetyObservationRepository? _safetyObservations;
    private ISafetyObservationResolutionRepository? _safetyResolutions;
    private ISafetyCategoryRepository? _safetyCategories;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Railroad Info
    // ──────────────────────────────────────────────────────────────────
    private IRailroadInformationRepository? _railroadInformation;
    private IRailroadInformationReadReceiptRepository? _railroadInformationReadReceipts;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Payroll
    // ──────────────────────────────────────────────────────────────────
    private ITimeEntryRepository? _timeEntries;
    private IPayrollRunRepository? _payrollRuns;
    private IPayrollRecordRepository? _payrollRecords;
    private IPayrollExportBatchRepository? _payrollExportBatches;
    private IPayrollImportRecordRepository? _payrollImportRecords;
    private IHolidayRepository? _holidays;
    private IHolidayQualificationRuleRepository? _holidayQualificationRules;
    private IHolidayPayrollRecordRepository? _holidayPayrollRecords;
    private IEarningCodeRuleRepository? _earningCodeRules;
    private IPayRateRepository? _payRates;
    private IRailroadHolidaySelectionRepository? _railroadHolidaySelections;
    private IRoleRepository? _roles;
    private IFeatureRepository? _features;
    private IPermissionRepository? _permissions;
    private IPositionVacancyRepository? _positionVacancies;
    private IBulletinRepository? _bulletins;
    private IBulletinBidRepository? _bulletinBids;

    public string CorrelationId { get; }
    public string OrchestrationId { get; }

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Core Employee
    // ──────────────────────────────────────────────────────────────────
    public IEmployeeRepository Employees => _employees ??= new EmployeeRepository(_crewContext, _currentUserService);
    public IParentRepository Parents => _parents ??= new ParentRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: ContactTypes
    // ──────────────────────────────────────────────────────────────────
    public IAddressTypeRepository AddressTypes => _addressTypes ??= new AddressTypeRepository(_crewContext, _currentUserService);
    public IPhoneNumberTypeRepository PhoneNumberTypes => _phoneNumberTypes ??= new PhoneNumberTypeRepository(_crewContext, _currentUserService);
    public IEmailAddressTypeRepository EmailAddressTypes => _emailAddressTypes ??= new EmailAddressTypeRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Employment
    // ──────────────────────────────────────────────────────────────────
    public IEmploymentStatusRepository EmploymentStatuses => _employmentStatuses ??= new EmploymentStatusRepository(_crewContext, _currentUserService);
    public IEmploymentStatusHistoryRepository EmploymentStatusHistory => _employmentStatusHistory ??= new EmploymentStatusHistoryRepository(_crewContext, _currentUserService);
    public IEmployeePriorServiceCreditRepository EmployeePriorServiceCredits => _employeePriorServiceCredits ??= new EmployeePriorServiceCreditRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Seniority
    // ──────────────────────────────────────────────────────────────────
    public ICraftRepository Crafts => _crafts ??= new CraftRepository(_crewContext, _currentUserService);
    public IRosterRepository Rosters => _rosters ??= new RosterRepository(_crewContext, _currentUserService);
    public ISeniorityRepository Seniority => _seniority ??= new SeniorityRepository(_crewContext, _currentUserService);
    public ISeniorityStateRepository SeniorityStates => _seniorityStates ??= new SeniorityStateRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: TenantConfig
    // ──────────────────────────────────────────────────────────────────
    public IGroupTypeRepository GroupTypes => _groupTypes ??= new GroupTypeRepository(_crewContext, _currentUserService);
    public IDynamicGroupRepository DynamicGroups => _dynamicGroups ??= new DynamicGroupRepository(_crewContext, _currentUserService);
    public IGroupAttributeDefinitionRepository AttributeDefinitions => _attributeDefinitions ??= new GroupAttributeDefinitionRepository(_crewContext, _currentUserService);
    public IGroupAttributeValueRepository AttributeValues => _attributeValues ??= new GroupAttributeValueRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Staffing
    // ──────────────────────────────────────────────────────────────────
    public IStaffablePositionRepository StaffablePositions => _staffablePositions ??= new StaffablePositionRepository(_crewContext, _currentUserService);
    public IPositionAssignmentRepository PositionAssignments => _positionAssignments ??= new PositionAssignmentRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Boards
    // ──────────────────────────────────────────────────────────────────
    public IBoardCascadePolicyRepository BoardCascadePolicies => _boardCascadePolicies ??= new BoardCascadePolicyRepository(_crewContext, _currentUserService);
    public IRosterBoardRepository RosterBoards => _rosterBoards ??= new RosterBoardRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Crews
    // ──────────────────────────────────────────────────────────────────
    public ICrewRepository Crews => _crews ??= new CrewRepository(_crewContext, _currentUserService);
    public ICrewPositionRepository CrewPositions => _crewPositions ??= new CrewPositionRepository(_crewContext, _currentUserService);
    public ICrewIncumbencyRepository CrewIncumbencies => _crewIncumbencies ??= new CrewIncumbencyRepository(_crewContext, _currentUserService);
    public ICrewAssignmentRepository CrewAssignments => _crewAssignments ??= new CrewAssignmentRepository(_crewContext, _currentUserService);
    public ICrewAttachmentInstanceRepository CrewAttachmentInstances => _crewAttachmentInstances ??= new CrewAttachmentInstanceRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Assignments
    // ──────────────────────────────────────────────────────────────────
    public IAssignmentRepository Assignments => _assignments ??= new AssignmentRepository(_crewContext, _currentUserService);
    public IAssignmentScheduleRepository AssignmentSchedules => _assignmentSchedules ??= new AssignmentScheduleRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: WorkManagement
    // ──────────────────────────────────────────────────────────────────
    public IDepartmentRepository Departments => _departments ??= new DepartmentRepository(_crewContext, _currentUserService);
    public ICraftRoleRepository CraftRoles => _craftRoles ??= new CraftRoleRepository(_crewContext, _currentUserService);
    public ICraftRoleQualificationRepository CraftRoleQualifications => _craftRoleQualifications ??= new CraftRoleQualificationRepository(_crewContext, _currentUserService);
    public IWorkInstanceRepository WorkInstances => _workInstances ??= new WorkInstanceRepository(_crewContext, _currentUserService);
    public IPositionSlotRepository PositionSlots => _positionSlots ??= new PositionSlotRepository(_crewContext, _currentUserService);
    public ISlotRequirementRepository SlotRequirements => _slotRequirements ??= new SlotRequirementRepository(_crewContext, _currentUserService);
    public IShiftDefinitionRepository ShiftDefinitions => _shiftDefinitions ??= new ShiftDefinitionRepository(_crewContext, _currentUserService);
    public IShiftInstanceRepository ShiftInstances => _shiftInstances ??= new ShiftInstanceRepository(_crewContext, _currentUserService);
    public IOnDutyRecordRepository OnDutyRecords => _onDutyRecords ??= new OnDutyRecordRepository(_crewContext, _currentUserService);
    public IOffDutyRecordRepository OffDutyRecords => _offDutyRecords ??= new OffDutyRecordRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Policies
    // ──────────────────────────────────────────────────────────────────
    public ICraftOperationsPolicyRepository CraftOperationsPolicies => _craftOperationsPolicies ??= new CraftOperationsPolicyRepository(_crewContext, _currentUserService);
    public ICraftDisplacementPolicyRepository CraftDisplacementPolicies => _craftDisplacementPolicies ??= new CraftDisplacementPolicyRepository(_crewContext, _currentUserService);
    public IDisplacementCaseRepository DisplacementCases => _displacementCases ??= new DisplacementCaseRepository(_crewContext, _currentUserService);
    public IDisplacementClaimRepository DisplacementClaims => _displacementClaims ??= new DisplacementClaimRepository(_crewContext, _currentUserService);
    public IBulletinPolicyRepository BulletinPolicies => _bulletinPolicies ??= new BulletinPolicyRepository(_crewContext, _currentUserService);
    public ISeniorityMovePolicyRepository SeniorityMovePolicies => _seniorityMovePolicies ??= new SeniorityMovePolicyRepository(_crewContext, _currentUserService);
    public ISeniorityMoveRepository SeniorityMoves => _seniorityMoves ??= new SeniorityMoveRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Dispatching
    // ──────────────────────────────────────────────────────────────────
    public IDispatchProjectionRepository DispatchProjections => _dispatchProjections ??= new DispatchProjectionRepository(_crewContext, _currentUserService);
    public IDispatchDecisionLogRepository DispatchDecisionLogs => _dispatchDecisionLogs ??= new DispatchDecisionLogRepository(_crewContext, _currentUserService);
    public IDispatchOverrideRepository DispatchOverrides => _dispatchOverrides ??= new DispatchOverrideRepository(_crewContext, _currentUserService);
    public IEmployeeBookingRepository EmployeeBookings => _employeeBookings ??= new EmployeeBookingRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Qualifications
    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: FRA Compliance
    // ──────────────────────────────────────────────────────────────────
    public IEmployeeCertificationRepository EmployeeCertifications => _employeeCertifications ??= new EmployeeCertificationRepository(_crewContext, _currentUserService);
    public IEmployeeCertificationReadRepository EmployeeCertificationReads => _employeeCertificationReads ??= new EmployeeCertificationReadRepository(_crewContext);
    public IFraCertificationConfigRepository FraCertificationConfigs => _fraCertificationConfigs ??= new FraCertificationConfigRepository(_crewContext, _currentUserService);
    public IFraCertificationCheckConfigRepository FraCertificationCheckConfigs => _fraCertificationCheckConfigs ??= new FraCertificationCheckConfigRepository(_crewContext, _currentUserService);
    public IFraDutyTourRepository FraDutyTours => _fraDutyTours ??= new FraDutyTourRepository(_crewContext, _currentUserService);
    public IRegulatoryStandardRepository RegulatoryStandards => _regulatoryStandards ??= new RegulatoryStandardRepository(_crewContext, _currentUserService);
    public IRegulatoryQualificationRepository RegulatoryQualifications => _regulatoryQualifications ??= new RegulatoryQualificationRepository(_crewContext, _currentUserService);
    public ICertificationRevocationRepository CertificationRevocations => _certificationRevocations ??= new CertificationRevocationRepository(_crewContext, _currentUserService);
    public IDrugAlcoholTestRepository DrugAlcoholTests => _drugAlcoholTests ??= new DrugAlcoholTestRepository(_crewContext, _currentUserService);
    public IDrugAlcoholActionRepository DrugAlcoholActions => _drugAlcoholActions ??= new DrugAlcoholActionRepository(_crewContext, _currentUserService);
    public IVoluntaryReferralRepository VoluntaryReferrals => _voluntaryReferrals ??= new VoluntaryReferralRepository(_crewContext, _currentUserService);

    public IQualificationTypeRepository QualificationTypes => _qualificationTypes ??= new QualificationTypeRepository(_crewContext, _currentUserService);
    public IQualificationRequirementRepository QualificationRequirements => _qualificationRequirements ??= new QualificationRequirementRepository(_crewContext, _currentUserService);
    public IEmployeeQualificationRepository EmployeeQualifications => _employeeQualifications ??= new EmployeeQualificationRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Absence & Vacancy
    // ──────────────────────────────────────────────────────────────────
    public IAbsenceRequestRepository AbsenceRequests => _absenceRequests ??= new AbsenceRequestRepository(_crewContext, _currentUserService);
    public IVacancyImpactRepository VacancyImpacts => _vacancyImpacts ??= new VacancyImpactRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Safety
    // ──────────────────────────────────────────────────────────────────
    public ISafetyObservationRepository SafetyObservations => _safetyObservations ??= new SafetyObservationRepository(_crewContext, _currentUserService);
    public ISafetyObservationResolutionRepository SafetyResolutions => _safetyResolutions ??= new SafetyObservationResolutionRepository(_crewContext, _currentUserService);
    public ISafetyCategoryRepository SafetyCategories => _safetyCategories ??= new SafetyCategoryRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Railroad Info
    // ──────────────────────────────────────────────────────────────────
    public IRailroadInformationRepository RailroadInformation => _railroadInformation ??= new RailroadInformationRepository(_crewContext, _currentUserService);
    public IRailroadInformationReadReceiptRepository RailroadInformationReadReceipts => _railroadInformationReadReceipts ??= new RailroadInformationReadReceiptRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Payroll
    // ──────────────────────────────────────────────────────────────────
    public ITimeEntryRepository TimeEntries => _timeEntries ??= new TimeEntryRepository(_crewContext, _currentUserService);
    public IPayrollRunRepository PayrollRuns => _payrollRuns ??= new PayrollRunRepository(_crewContext, _currentUserService);
    public IPayrollRecordRepository PayrollRecords => _payrollRecords ??= new PayrollRecordRepository(_crewContext, _currentUserService);
    public IPayrollExportBatchRepository PayrollExportBatches => _payrollExportBatches ??= new PayrollExportBatchRepository(_crewContext, _currentUserService);
    public IPayrollImportRecordRepository PayrollImportRecords => _payrollImportRecords ??= new PayrollImportRecordRepository(_crewContext, _currentUserService);
    public IHolidayRepository Holidays => _holidays ??= new HolidayRepository(_crewContext, _currentUserService);
    public IHolidayQualificationRuleRepository HolidayQualificationRules => _holidayQualificationRules ??= new HolidayQualificationRuleRepository(_crewContext, _currentUserService);
    public IHolidayPayrollRecordRepository HolidayPayrollRecords => _holidayPayrollRecords ??= new HolidayPayrollRecordRepository(_crewContext, _currentUserService);
    public IEarningCodeRuleRepository EarningCodeRules => _earningCodeRules ??= new EarningCodeRuleRepository(_crewContext, _currentUserService);
    public IPayRateRepository PayRates => _payRates ??= new PayRateRepository(_crewContext, _currentUserService);
    public IRailroadHolidaySelectionRepository RailroadHolidaySelections => _railroadHolidaySelections ??= new RailroadHolidaySelectionRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Authorization
    // ──────────────────────────────────────────────────────────────────
    public IRoleRepository Roles => _roles ??= new RoleRepository(_crewContext, _currentUserService);
    public IFeatureRepository Features => _features ??= new FeatureRepository(_crewContext, _currentUserService);
    public IPermissionRepository Permissions => _permissions ??= new PermissionRepository(_crewContext, _currentUserService);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Bulletins
    // ──────────────────────────────────────────────────────────────────
    public IPositionVacancyRepository PositionVacancies => _positionVacancies ??= new PositionVacancyRepository(_crewContext, _currentUserService);
    public IBulletinRepository Bulletins => _bulletins ??= new BulletinRepository(_crewContext, _currentUserService);
    public IBulletinBidRepository BulletinBids => _bulletinBids ??= new BulletinBidRepository(_crewContext, _currentUserService);

    internal OrchestrationUnitOfWork(
        DbConnection connection,
        DbTransaction transaction,
        CrewServiceDbContext crewContext,
        ICurrentUserService currentUserService,
        string correlationId,
        string orchestrationId,
        string? idempotencyKey,
        ILogger<OrchestrationUnitOfWork> logger,
        IOutboxDispatcher? dispatcher = null)
    {
        _connection = connection;
        _transaction = transaction;
        _crewContext = crewContext;
        _currentUserService = currentUserService;
        CorrelationId = correlationId;
        OrchestrationId = orchestrationId;
        _idempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString();
        _logger = logger;
        _dispatcher = dispatcher;
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(OrchestrationUnitOfWork));

        var domainEvents = CollectDomainEvents();
        foreach (var domainEvent in domainEvents)
            _crewContext.OutboxMessages.Add(CreateOutboxMessage(domainEvent));

        await _crewContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_committed)
            throw new InvalidOperationException("Transaction has already been committed.");

        ObjectDisposedException.ThrowIf(_disposed, typeof(OrchestrationUnitOfWork));

        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Committing orchestration UoW. CorrelationId: {CorrelationId}, OrchestrationId: {OrchestrationId}",
                    CorrelationId, OrchestrationId);
            }

            // Collect domain events from all tracked entities
            var domainEvents = CollectDomainEvents();

            // Convert domain events to OutboxMessage rows
            var outboxMessages = new List<OutboxMessage>();
            foreach (var domainEvent in domainEvents)
            {
                var outboxMessage = CreateOutboxMessage(domainEvent);
                outboxMessages.Add(outboxMessage);
                _crewContext.OutboxMessages.Add(outboxMessage);
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var eventCount = domainEvents.Count;
                _logger.LogDebug(
                    "Persisting {EventCount} domain events to outbox. OrchestrationId: {OrchestrationId}",
                    eventCount, OrchestrationId);
            }

            // Save all entity changes + outbox rows in single SaveChanges
            await _crewContext.SaveChangesAsync(cancellationToken);

            // Commit the shared transaction
            await _transaction.CommitAsync(cancellationToken);

            _committed = true;

            // Dispatch messages for immediate publishing (if dispatcher available)
            if (_dispatcher is not null && outboxMessages.Count > 0)
            {
                _dispatcher.EnqueueForDispatch(outboxMessages);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Dispatched {Count} messages for immediate publishing.", outboxMessages.Count);
                }
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                var outboxCount = outboxMessages.Count;
                _logger.LogInformation(
                    "Orchestration UoW committed successfully. CorrelationId: {CorrelationId}, OrchestrationId: {OrchestrationId}, EventsWritten: {EventCount}",
                    CorrelationId, OrchestrationId, outboxCount);
            }
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex,
                    "Orchestration UoW commit failed. CorrelationId: {CorrelationId}, OrchestrationId: {OrchestrationId}",
                    CorrelationId, OrchestrationId);
            }

            await RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_committed || _disposed)
            return;

        try
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Rolling back orchestration UoW. CorrelationId: {CorrelationId}, OrchestrationId: {OrchestrationId}",
                    CorrelationId, OrchestrationId);
            }

            await _transaction.RollbackAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex,
                    "Orchestration UoW rollback failed. CorrelationId: {CorrelationId}, OrchestrationId: {OrchestrationId}",
                    CorrelationId, OrchestrationId);
            }
        }
    }

    private List<DomainEvent> CollectDomainEvents()
    {
        var domainEvents = new List<DomainEvent>();

        // Collect from all tracked Entity instances
        var trackedEntities = _crewContext.ChangeTracker
            .Entries<Entity>()
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in trackedEntities)
        {
            var events = entity.DomainEvents;
            foreach (var domainEvent in events.OfType<DomainEvent>())
            {
                // Enrich event with correlation/orchestration IDs
                var enrichedEvent = domainEvent with
                {
                    CorrelationId = CorrelationId,
                    OrchestrationId = OrchestrationId,
                    IdempotencyKey = $"{_idempotencyKey}:{domainEvent.EventType}:{domainEvent.AggregateId}"
                };
                domainEvents.Add(enrichedEvent);
            }
        }

        return domainEvents;
    }

    private static OutboxMessage CreateOutboxMessage(DomainEvent domainEvent)
    {
        return OutboxMessage.Create(
            messageId: domainEvent.EventId,
            eventType: domainEvent.EventType,
            aggregateType: domainEvent.AggregateType,
            aggregateId: domainEvent.AggregateId,
            payloadJson: domainEvent.ToString(),
            correlationId: domainEvent.CorrelationId,
            orchestrationId: domainEvent.OrchestrationId,
            idempotencyKey: domainEvent.IdempotencyKey,
            eventVersion: domainEvent.EventVersion);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeAsyncCore().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    private async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (!_committed)
        {
            await RollbackAsync();
        }

        await _crewContext.DisposeAsync();
        await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
