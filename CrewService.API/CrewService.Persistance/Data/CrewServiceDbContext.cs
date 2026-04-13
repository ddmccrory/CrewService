using CrewService.Domain.DomainEvents;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Outbox;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using System.Text.Json;

namespace CrewService.Persistance.Data;

internal sealed class CrewServiceDbContext(
DbContextOptions<CrewServiceDbContext> options,
ICurrentUserService currentUserService,
IFieldEncryptor fieldEncryptor) : DbContext(options), IOutboxDbContext
{
    private static readonly JsonSerializerOptions s_camelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly string[] s_auditPrefixes = ["CreatedBy", "ModifiedBy", "DeletedBy", "IsDeleted", "DeletedAt"];

    // Legacy Employees
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<AddressType> AddressTypes => Set<AddressType>();
    public DbSet<Craft> Crafts => Set<Craft>();
    public DbSet<EmailAddress> EmailAddresses => Set<EmailAddress>();
    public DbSet<EmailAddressType> EmailAddressTypes => Set<EmailAddressType>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeePriorServiceCredit> EmployeePriorServiceCredits => Set<EmployeePriorServiceCredit>();
    public DbSet<EmploymentStatus> EmploymentStatuses => Set<EmploymentStatus>();
    public DbSet<EmploymentStatusHistory> EmploymentStatusHistory => Set<EmploymentStatusHistory>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<PhoneNumber> PhoneNumbers => Set<PhoneNumber>();
    public DbSet<PhoneNumberType> PhoneNumberTypes => Set<PhoneNumberType>();
    public DbSet<PayrollTier> PayrollTiers => Set<PayrollTier>();
    public DbSet<Roster> Rosters => Set<Roster>();
    public DbSet<Seniority> Seniority => Set<Seniority>();
    public DbSet<SeniorityState> SeniorityStates => Set<SeniorityState>();
    public DbSet<UserParentAssignment> UserParentAssignments => Set<UserParentAssignment>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    // TenantConfig Module
    public DbSet<GroupType> GroupTypes => Set<GroupType>();
    public DbSet<DynamicGroup> DynamicGroups => Set<DynamicGroup>();
    public DbSet<GroupAttributeDefinition> GroupAttributeDefinitions => Set<GroupAttributeDefinition>();
    public DbSet<GroupAttributeValue> GroupAttributeValues => Set<GroupAttributeValue>();
    public DbSet<TeamsWebhookConfig> TeamsWebhookConfigs => Set<TeamsWebhookConfig>();

    // WorkManagement Module
    public DbSet<Domain.Modules.WorkManagement.WorkInstance> WorkInstances => Set<Domain.Modules.WorkManagement.WorkInstance>();
    public DbSet<Domain.Modules.WorkManagement.CraftRole> CraftRoles => Set<Domain.Modules.WorkManagement.CraftRole>();
    public DbSet<Domain.Modules.WorkManagement.PositionSlot> PositionSlots => Set<Domain.Modules.WorkManagement.PositionSlot>();
    public DbSet<Domain.Modules.WorkManagement.SlotRequirement> SlotRequirements => Set<Domain.Modules.WorkManagement.SlotRequirement>();
    public DbSet<Domain.Modules.WorkManagement.ShiftDefinition> ShiftDefinitions => Set<Domain.Modules.WorkManagement.ShiftDefinition>();
    public DbSet<Domain.Modules.WorkManagement.ShiftInstance> ShiftInstances => Set<Domain.Modules.WorkManagement.ShiftInstance>();
    public DbSet<Domain.Modules.WorkManagement.PositionSlotInstance> PositionSlotInstances => Set<Domain.Modules.WorkManagement.PositionSlotInstance>();
    public DbSet<Domain.Modules.WorkManagement.BoardSlotInstance> BoardSlotInstances => Set<Domain.Modules.WorkManagement.BoardSlotInstance>();
    public DbSet<Domain.Modules.WorkManagement.AbolishmentRecord> AbolishmentRecords => Set<Domain.Modules.WorkManagement.AbolishmentRecord>();

    // Crews Module
    public DbSet<Domain.Modules.Crews.Crew> Crews => Set<Domain.Modules.Crews.Crew>();
    public DbSet<Domain.Modules.Crews.CrewPosition> CrewPositions => Set<Domain.Modules.Crews.CrewPosition>();
    public DbSet<Domain.Modules.Crews.CrewIncumbency> CrewIncumbencies => Set<Domain.Modules.Crews.CrewIncumbency>();
    public DbSet<Domain.Modules.Crews.CrewAssignment> CrewAssignments => Set<Domain.Modules.Crews.CrewAssignment>();
    public DbSet<Domain.Modules.Crews.CrewAttachmentInstance> CrewAttachmentInstances => Set<Domain.Modules.Crews.CrewAttachmentInstance>();
    public DbSet<Domain.Modules.Crews.Assignment> Assignments => Set<Domain.Modules.Crews.Assignment>();
    public DbSet<Domain.Modules.Crews.AssignmentSchedule> AssignmentSchedules => Set<Domain.Modules.Crews.AssignmentSchedule>();

    // Bulletins Module
    public DbSet<Domain.Modules.Bulletins.PositionVacancy> PositionVacancies => Set<Domain.Modules.Bulletins.PositionVacancy>();
    public DbSet<Domain.Modules.Bulletins.Bulletin> Bulletins => Set<Domain.Modules.Bulletins.Bulletin>();
    public DbSet<Domain.Modules.Bulletins.BulletinBid> BulletinBids => Set<Domain.Modules.Bulletins.BulletinBid>();

    // Dispatching Module
    public DbSet<Domain.Modules.Dispatching.ChangeNotification> ChangeNotifications => Set<Domain.Modules.Dispatching.ChangeNotification>();
    public DbSet<Domain.Modules.Dispatching.OnDutyRecord> OnDutyRecords => Set<Domain.Modules.Dispatching.OnDutyRecord>();
    public DbSet<Domain.Modules.Dispatching.OffDutyRecord> OffDutyRecords => Set<Domain.Modules.Dispatching.OffDutyRecord>();
    public DbSet<Domain.Modules.Dispatching.OnDutyBillingRecord> OnDutyBillingRecords => Set<Domain.Modules.Dispatching.OnDutyBillingRecord>();
    public DbSet<Domain.Modules.Dispatching.OnDutyLocomotiveRecord> OnDutyLocomotiveRecords => Set<Domain.Modules.Dispatching.OnDutyLocomotiveRecord>();
    public DbSet<Domain.Modules.Dispatching.OnDutyMaterialRecord> OnDutyMaterialRecords => Set<Domain.Modules.Dispatching.OnDutyMaterialRecord>();
    public DbSet<Domain.Modules.Dispatching.VacancyResolutionRun> VacancyResolutionRuns => Set<Domain.Modules.Dispatching.VacancyResolutionRun>();
    public DbSet<Domain.Modules.Dispatching.DailyEmployeeStatusRecord> DailyEmployeeStatusRecords => Set<Domain.Modules.Dispatching.DailyEmployeeStatusRecord>();
    public DbSet<Domain.Modules.Dispatching.DispatchProjection> DispatchProjections => Set<Domain.Modules.Dispatching.DispatchProjection>();
    public DbSet<Domain.Modules.Dispatching.DispatchDecisionLog> DispatchDecisionLogs => Set<Domain.Modules.Dispatching.DispatchDecisionLog>();
    public DbSet<Domain.Modules.Dispatching.DispatchOverride> DispatchOverrides => Set<Domain.Modules.Dispatching.DispatchOverride>();
    public DbSet<Domain.Modules.Dispatching.EmployeeBooking> EmployeeBookings => Set<Domain.Modules.Dispatching.EmployeeBooking>();

    // FraCompliance Module
    public DbSet<Domain.Modules.FraCompliance.RegulatoryStandard> RegulatoryStandards => Set<Domain.Modules.FraCompliance.RegulatoryStandard>();
    public DbSet<Domain.Modules.FraCompliance.FraDutyTour> FraDutyTours => Set<Domain.Modules.FraCompliance.FraDutyTour>();
    public DbSet<Domain.Modules.FraCompliance.FraDutyTourSegment> FraDutyTourSegments => Set<Domain.Modules.FraCompliance.FraDutyTourSegment>();
    public DbSet<Domain.Modules.FraCompliance.FraTransportationSegment> FraTransportationSegments => Set<Domain.Modules.FraCompliance.FraTransportationSegment>();
    public DbSet<Domain.Modules.FraCompliance.FraOtherServiceSegment> FraOtherServiceSegments => Set<Domain.Modules.FraCompliance.FraOtherServiceSegment>();
    public DbSet<Domain.Modules.FraCompliance.FraExcessServiceReport> FraExcessServiceReports => Set<Domain.Modules.FraCompliance.FraExcessServiceReport>();
    public DbSet<Domain.Modules.FraCompliance.FraMonthlyAccumulator> FraMonthlyAccumulators => Set<Domain.Modules.FraCompliance.FraMonthlyAccumulator>();
    public DbSet<Domain.Modules.FraCompliance.RegulatoryQualification> RegulatoryQualifications => Set<Domain.Modules.FraCompliance.RegulatoryQualification>();
    public DbSet<Domain.Modules.FraCompliance.CraftRegulatoryQualification> CraftRegulatoryQualifications => Set<Domain.Modules.FraCompliance.CraftRegulatoryQualification>();
    public DbSet<Domain.Modules.FraCompliance.EmployeeCertification> EmployeeCertifications => Set<Domain.Modules.FraCompliance.EmployeeCertification>();
    public DbSet<Domain.Modules.FraCompliance.CertificationEligibilityCheck> CertificationEligibilityChecks => Set<Domain.Modules.FraCompliance.CertificationEligibilityCheck>();
    public DbSet<Domain.Modules.FraCompliance.CertificationRevocationRecord> CertificationRevocationRecords => Set<Domain.Modules.FraCompliance.CertificationRevocationRecord>();
    public DbSet<Domain.Modules.FraCompliance.DrugAlcoholTestRecord> DrugAlcoholTestRecords => Set<Domain.Modules.FraCompliance.DrugAlcoholTestRecord>();
    public DbSet<Domain.Modules.FraCompliance.DrugAlcoholAction> DrugAlcoholActions => Set<Domain.Modules.FraCompliance.DrugAlcoholAction>();
    public DbSet<Domain.Modules.FraCompliance.VoluntaryReferral> VoluntaryReferrals => Set<Domain.Modules.FraCompliance.VoluntaryReferral>();

    // Policies Module
    public DbSet<Domain.Modules.Policies.CraftOperationsPolicy> CraftOperationsPolicies => Set<Domain.Modules.Policies.CraftOperationsPolicy>();
    public DbSet<Domain.Modules.Policies.CraftDisplacementPolicy> CraftDisplacementPolicies => Set<Domain.Modules.Policies.CraftDisplacementPolicy>();
    public DbSet<Domain.Modules.Policies.DisplacementCase> DisplacementCases => Set<Domain.Modules.Policies.DisplacementCase>();
    public DbSet<Domain.Modules.Policies.DisplacementClaim> DisplacementClaims => Set<Domain.Modules.Policies.DisplacementClaim>();
    public DbSet<Domain.Modules.Policies.BulletinPolicy> BulletinPolicies => Set<Domain.Modules.Policies.BulletinPolicy>();
    public DbSet<Domain.Modules.Policies.SeniorityMovePolicy> SeniorityMovePolicies => Set<Domain.Modules.Policies.SeniorityMovePolicy>();
    public DbSet<Domain.Modules.Policies.SeniorityMove> SeniorityMoves => Set<Domain.Modules.Policies.SeniorityMove>();

    // Infrastructure Module
    public DbSet<Domain.Modules.Infrastructure.WorkerSchedule> WorkerSchedules => Set<Domain.Modules.Infrastructure.WorkerSchedule>();
    public DbSet<Domain.Modules.Infrastructure.WorkerExecutionLog> WorkerExecutionLogs => Set<Domain.Modules.Infrastructure.WorkerExecutionLog>();
    public DbSet<Domain.Modules.Infrastructure.ProcessingLock> ProcessingLocks => Set<Domain.Modules.Infrastructure.ProcessingLock>();

    // Staffing Module
    public DbSet<Domain.Modules.Staffing.StaffablePosition> StaffablePositions => Set<Domain.Modules.Staffing.StaffablePosition>();
    public DbSet<Domain.Modules.Staffing.PositionAssignment> PositionAssignments => Set<Domain.Modules.Staffing.PositionAssignment>();

    // Boards Module
    public DbSet<Domain.Modules.Boards.RosterBoard> RosterBoards => Set<Domain.Modules.Boards.RosterBoard>();
    public DbSet<Domain.Modules.Boards.RosterBoardPosition> RosterBoardPositions => Set<Domain.Modules.Boards.RosterBoardPosition>();
    public DbSet<Domain.Modules.Boards.BoardCascadePolicy> BoardCascadePolicies => Set<Domain.Modules.Boards.BoardCascadePolicy>();

    // Audit
    public DbSet<DomainEventLog> DomainEventLogs => Set<DomainEventLog>();

    // Payroll Module
    public DbSet<Domain.Modules.Payroll.EarningCodeRule> EarningCodeRules => Set<Domain.Modules.Payroll.EarningCodeRule>();
    public DbSet<Domain.Modules.Payroll.PayRate> PayRates => Set<Domain.Modules.Payroll.PayRate>();
    public DbSet<Domain.Modules.Payroll.EarningApproval> EarningApprovals => Set<Domain.Modules.Payroll.EarningApproval>();
    public DbSet<Domain.Modules.Payroll.TimeEntry> TimeEntries => Set<Domain.Modules.Payroll.TimeEntry>();
    public DbSet<Domain.Modules.Payroll.PayrollRun> PayrollRuns => Set<Domain.Modules.Payroll.PayrollRun>();
    public DbSet<Domain.Modules.Payroll.PayrollRecord> PayrollRecords => Set<Domain.Modules.Payroll.PayrollRecord>();
    public DbSet<Domain.Modules.Payroll.PayrollExportBatch> PayrollExportBatches => Set<Domain.Modules.Payroll.PayrollExportBatch>();
    public DbSet<Domain.Modules.Payroll.PayrollImportRecord> PayrollImportRecords => Set<Domain.Modules.Payroll.PayrollImportRecord>();
    public DbSet<Domain.Modules.Payroll.Holiday> Holidays => Set<Domain.Modules.Payroll.Holiday>();
    public DbSet<Domain.Modules.Payroll.HolidayQualificationRule> HolidayQualificationRules => Set<Domain.Modules.Payroll.HolidayQualificationRule>();
    public DbSet<Domain.Modules.Payroll.HolidayPayrollRecord> HolidayPayrollRecords => Set<Domain.Modules.Payroll.HolidayPayrollRecord>();
    public DbSet<Domain.Modules.HolidayManagement.RailroadHolidaySelection> RailroadHolidaySelections => Set<Domain.Modules.HolidayManagement.RailroadHolidaySelection>();

    // Notifications Module
    public DbSet<Domain.Modules.Notifications.NotificationRequest> NotificationRequests => Set<Domain.Modules.Notifications.NotificationRequest>();
    public DbSet<Domain.Modules.Notifications.NotificationResponse> NotificationResponses => Set<Domain.Modules.Notifications.NotificationResponse>();
    public DbSet<Domain.Modules.Notifications.NotificationProviderConfig> NotificationProviderConfigs => Set<Domain.Modules.Notifications.NotificationProviderConfig>();

    // AbsenceVacancy Module
    public DbSet<Domain.Modules.AbsenceVacancy.AbsenceCode> AbsenceCodes => Set<Domain.Modules.AbsenceVacancy.AbsenceCode>();
    public DbSet<Domain.Modules.AbsenceVacancy.AbsenceCodeCraftOverride> AbsenceCodeCraftOverrides => Set<Domain.Modules.AbsenceVacancy.AbsenceCodeCraftOverride>();
    public DbSet<Domain.Modules.AbsenceVacancy.AbsenceRequest> AbsenceRequests => Set<Domain.Modules.AbsenceVacancy.AbsenceRequest>();
    public DbSet<Domain.Modules.AbsenceVacancy.AbsenceApproval> AbsenceApprovals => Set<Domain.Modules.AbsenceVacancy.AbsenceApproval>();
    public DbSet<Domain.Modules.AbsenceVacancy.AbsenceMarkUp> AbsenceMarkUps => Set<Domain.Modules.AbsenceVacancy.AbsenceMarkUp>();
    public DbSet<Domain.Modules.AbsenceVacancy.CompensationBalance> CompensationBalances => Set<Domain.Modules.AbsenceVacancy.CompensationBalance>();
    public DbSet<Domain.Modules.AbsenceVacancy.VacancyImpact> VacancyImpacts => Set<Domain.Modules.AbsenceVacancy.VacancyImpact>();

    // Safety Module
    public DbSet<Domain.Modules.Safety.SafetyObservation> SafetyObservations => Set<Domain.Modules.Safety.SafetyObservation>();
    public DbSet<Domain.Modules.Safety.SafetyObservationAction> SafetyObservationActions => Set<Domain.Modules.Safety.SafetyObservationAction>();
    public DbSet<Domain.Modules.Safety.SafetyObservationResolution> SafetyObservationResolutions => Set<Domain.Modules.Safety.SafetyObservationResolution>();
    public DbSet<Domain.Modules.Safety.SafetyCategory> SafetyCategories => Set<Domain.Modules.Safety.SafetyCategory>();

    // Authorization Module
    public DbSet<Domain.Modules.Authorization.Role> Roles => Set<Domain.Modules.Authorization.Role>();
    public DbSet<Domain.Modules.Authorization.Feature> Features => Set<Domain.Modules.Authorization.Feature>();
    public DbSet<Domain.Modules.Authorization.Permission> Permissions => Set<Domain.Modules.Authorization.Permission>();

    // RailroadInfo Module
    public DbSet<Domain.Modules.RailroadInfo.RailroadInformation> RailroadInformations => Set<Domain.Modules.RailroadInfo.RailroadInformation>();
    public DbSet<Domain.Modules.RailroadInfo.RailroadInformationReadReceipt> RailroadInformationReadReceipts => Set<Domain.Modules.RailroadInfo.RailroadInformationReadReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrewServiceDbContext).Assembly);

        // ControlNumber is a value object handled via per-property conversions;
        // prevent EF from discovering it as a complex/entity type.
        modelBuilder.Ignore<ControlNumber>();

        // Encrypt sensitive PII fields at rest
        var encryptedConverter = new EncryptedStringConverter(fieldEncryptor);
        modelBuilder.Entity<Employee>()
            .Property(e => e.SocialSecurityNumber)
            .HasConversion(encryptedConverter);
        modelBuilder.Entity<Employee>()
            .Property(e => e.DriversLicenseNumber)
            .HasConversion(
                v => v == null ? null : fieldEncryptor.Encrypt(v),
                v => v == null ? null : fieldEncryptor.Decrypt(v));

        // Apply global soft-delete filter to all Entity types
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(Entity.IsDeleted));
                var filterExpression = Expression.Equal(property, Expression.Constant(false));
                var lambda = Expression.Lambda(filterExpression, parameter);

                entityType.SetQueryFilter(lambda);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        CollectDomainEventLogs();
        await CascadeSoftDeletesAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Collects domain events from all tracked entities, creates permanent
    /// <see cref="DomainEventLog"/> audit records, and clears the events from entities.
    /// For entities that do not raise explicit events, auto-generates audit entries
    /// from the EF change tracker (Added / Modified / soft-Deleted).
    /// </summary>
    private void CollectDomainEventLogs()
    {
        var trackedEntries = ChangeTracker.Entries<Entity>().ToList();

        var performedBy = currentUserService.GetUserName();
        var parentCtrlNbr = currentUserService.GetParentCtrlNbr();

        foreach (var entry in trackedEntries)
        {
            var entity = entry.Entity;
            var events = entity.DomainEvents;

            // ── Explicit domain events (richer payloads) ──
            if (events.Count > 0)
            {
                foreach (var domainEvent in events.OfType<DomainEvent>())
                {
                    var log = DomainEventLog.Create(
                        domainEvent.EventId,
                        domainEvent.EventType,
                        domainEvent.AggregateType,
                        domainEvent.AggregateId,
                        domainEvent.OccurredAt,
                        domainEvent.PayloadJson,
                        performedBy,
                        parentCtrlNbr);
                    DomainEventLogs.Add(log);
                }
                entity.ClearDomainEvents();
                continue;
            }

            // ── Auto-generate audit entry from change tracker ──
            var aggregateType = entity.GetType().Name;
            var aggregateId = entity.CtrlNbr.Value;
            string? eventType = null;
            string? payloadJson = null;

            switch (entry.State)
            {
                case EntityState.Added:
                    eventType = $"{aggregateType}Created";
                    break;

                case EntityState.Deleted:
                    eventType = $"{aggregateType}Deleted";
                    break;

                case EntityState.Modified:
                    // Detect soft-delete transition
                    var wasDeleted = entry.OriginalValues.GetValue<bool>(nameof(Entity.IsDeleted));
                    if (entity.IsDeleted && !wasDeleted)
                    {
                        eventType = $"{aggregateType}Deleted";
                    }
                    else
                    {
                        var changes = new Dictionary<string, object?>();
                        foreach (var prop in entry.Properties)
                        {
                            if (!prop.IsModified) continue;
                            if (s_auditPrefixes.Any(p => prop.Metadata.Name.StartsWith(p))) continue;

                            var value = prop.CurrentValue;
                            if (value is ControlNumber cn)
                                value = cn.Value;

                            changes[prop.Metadata.Name] = value;
                        }

                        if (changes.Count == 0) continue;

                        eventType = $"{aggregateType}Updated";
                        payloadJson = JsonSerializer.Serialize(changes, s_camelCase);
                    }
                    break;
            }

            if (eventType is null) continue;

            var autoLog = DomainEventLog.Create(
                Guid.NewGuid(),
                eventType,
                aggregateType,
                aggregateId,
                DateTime.UtcNow,
                payloadJson,
                performedBy,
                parentCtrlNbr);
            DomainEventLogs.Add(autoLog);
        }
    }

    /// <summary>
    /// Automatically cascades soft-deletes to dependent entities whose FK relationship
    /// is configured with DeleteBehavior.Cascade, mirroring what EF does for hard deletes.
    /// Uses BFS to handle cascading chains (e.g., Crew → CrewPosition → CrewIncumbency).
    /// </summary>
    private async Task CascadeSoftDeletesAsync(CancellationToken cancellationToken)
    {
        var newlyDeletedEntries = ChangeTracker.Entries<Entity>()
            .Where(e => e.State == EntityState.Modified
                && e.Entity.IsDeleted
                && !e.OriginalValues.GetValue<bool>(nameof(Entity.IsDeleted)))
            .ToList();

        if (newlyDeletedEntries.Count == 0) return;

        var auditUser = currentUserService.GetUserName();
        var now = DateTime.UtcNow;

        var queue = new Queue<(IEntityType EntityType, long PkValue)>();
        foreach (var entry in newlyDeletedEntries)
            queue.Enqueue((entry.Metadata, entry.Entity.CtrlNbr.Value));

        while (queue.Count > 0)
        {
            var (parentType, parentPk) = queue.Dequeue();

            var cascadeFKs = Model.GetEntityTypes()
                .Where(et => typeof(Entity).IsAssignableFrom(et.ClrType))
                .SelectMany(et => et.GetForeignKeys())
                .Where(fk => fk.PrincipalEntityType.ClrType == parentType.ClrType
                    && fk.DeleteBehavior == DeleteBehavior.Cascade);

            foreach (var fk in cascadeFKs)
            {
                var depType = fk.DeclaringEntityType;
                var tableName = depType.GetTableName()!;
                var storeId = StoreObjectIdentifier.Table(tableName, depType.GetSchema());
                var fkCol = fk.Properties[0].GetColumnName(storeId)!;
                var pkCol = depType.FindPrimaryKey()!.Properties[0].GetColumnName(storeId)!;

                var selectSql = $"SELECT \"{pkCol}\" AS \"Value\" FROM \"{tableName}\" WHERE \"{fkCol}\" = {{0}} AND IsDeleted = 0";
                var dependentPks = await Database
                    .SqlQueryRaw<long>(selectSql, parentPk)
                    .ToListAsync(cancellationToken);

                if (dependentPks.Count == 0) continue;

                var updateSql =
                    $"UPDATE \"{tableName}\" SET IsDeleted = 1, DeletedAt = {{0}}, " +
                    $"DeletedBy_AuditName = {{1}}, DeletedBy_AuditDateTime = {{2}}, " +
                    $"ModifiedBy_AuditName = {{3}}, ModifiedBy_AuditDateTime = {{4}} " +
                    $"WHERE \"{fkCol}\" = {{5}} AND IsDeleted = 0";
                await Database.ExecuteSqlRawAsync(
                    updateSql,
                    new object[] { now, auditUser, now, auditUser, now, parentPk },
                    cancellationToken);

                foreach (var depPk in dependentPks)
                    queue.Enqueue((depType, depPk));
            }
        }
    }

    private void UpdateAuditableEntities()
    {
        var auditableEntries = ChangeTracker.Entries<Entity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .ToList();

        if (auditableEntries.Count == 0)
            return;

        string auditName = currentUserService.GetUserName();

        if (string.IsNullOrWhiteSpace(auditName))
            throw new InvalidOperationException("Audit name cannot be null or empty. Ensure user context is available.");

        foreach (var entry in auditableEntries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = AuditStamp.Create(auditName);
                    entry.Entity.ModifiedBy = AuditStamp.Create(auditName);
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedBy = AuditStamp.Create(auditName);
                    break;
            }
        }
    }
}
