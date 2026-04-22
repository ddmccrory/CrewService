using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;

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
    // Core Employee Orchestration
    // ──────────────────────────────────────────────────────────────────
    IEmployeeRepository Employees { get; }
    IParentRepository Parents { get; }

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
    IRosterBoardRepository RosterBoards { get; }

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
    IWorkInstanceRepository WorkInstances { get; }
    IPositionSlotRepository PositionSlots { get; }
    ISlotRequirementRepository SlotRequirements { get; }
    IShiftDefinitionRepository ShiftDefinitions { get; }

    // ──────────────────────────────────────────────────────────────────
    // Qualifications
    // ──────────────────────────────────────────────────────────────────
    IQualificationTypeRepository QualificationTypes { get; }
    IQualificationRequirementRepository QualificationRequirements { get; }
    IEmployeeQualificationRepository EmployeeQualifications { get; }

    /// <summary>
    /// Collects domain events from tracked entities, persists OutboxMessage rows,
    /// saves all changes, and commits the transaction atomically.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction. Call on error before disposing.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}