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
using System.Linq.Expressions;

namespace CrewService.Persistance.Data;

internal sealed class CrewServiceDbContext(
DbContextOptions<CrewServiceDbContext> options,
ICurrentUserService currentUserService,
IFieldEncryptor fieldEncryptor) : DbContext(options), IOutboxDbContext
{
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
    public DbSet<Railroad> Railroads => Set<Railroad>();
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
    public DbSet<RailroadGroupPlacement> RailroadGroupPlacements => Set<RailroadGroupPlacement>();
    public DbSet<TeamsWebhookConfig> TeamsWebhookConfigs => Set<TeamsWebhookConfig>();

    // WorkManagement Module
    public DbSet<Domain.Modules.WorkManagement.AssignmentTemplate> AssignmentTemplates => Set<Domain.Modules.WorkManagement.AssignmentTemplate>();
    public DbSet<Domain.Modules.WorkManagement.WorkInstance> WorkInstances => Set<Domain.Modules.WorkManagement.WorkInstance>();
    public DbSet<Domain.Modules.WorkManagement.PositionRole> PositionRoles => Set<Domain.Modules.WorkManagement.PositionRole>();
    public DbSet<Domain.Modules.WorkManagement.PositionSlot> PositionSlots => Set<Domain.Modules.WorkManagement.PositionSlot>();
    public DbSet<Domain.Modules.WorkManagement.SlotRequirement> SlotRequirements => Set<Domain.Modules.WorkManagement.SlotRequirement>();
    public DbSet<Domain.Modules.WorkManagement.ShiftDefinition> ShiftDefinitions => Set<Domain.Modules.WorkManagement.ShiftDefinition>();
    public DbSet<Domain.Modules.WorkManagement.ShiftInstance> ShiftInstances => Set<Domain.Modules.WorkManagement.ShiftInstance>();
    public DbSet<Domain.Modules.WorkManagement.PositionSlotInstance> PositionSlotInstances => Set<Domain.Modules.WorkManagement.PositionSlotInstance>();
    public DbSet<Domain.Modules.WorkManagement.CrewOffDay> CrewOffDays => Set<Domain.Modules.WorkManagement.CrewOffDay>();
    public DbSet<Domain.Modules.WorkManagement.AbolishmentRecord> AbolishmentRecords => Set<Domain.Modules.WorkManagement.AbolishmentRecord>();

    // Dispatching Module
    public DbSet<Domain.Modules.Dispatching.ChangeNotification> ChangeNotifications => Set<Domain.Modules.Dispatching.ChangeNotification>();
    public DbSet<Domain.Modules.Dispatching.OnDutyRecord> OnDutyRecords => Set<Domain.Modules.Dispatching.OnDutyRecord>();
    public DbSet<Domain.Modules.Dispatching.OffDutyRecord> OffDutyRecords => Set<Domain.Modules.Dispatching.OffDutyRecord>();
    public DbSet<Domain.Modules.Dispatching.OnDutyBillingRecord> OnDutyBillingRecords => Set<Domain.Modules.Dispatching.OnDutyBillingRecord>();
    public DbSet<Domain.Modules.Dispatching.OnDutyLocomotiveRecord> OnDutyLocomotiveRecords => Set<Domain.Modules.Dispatching.OnDutyLocomotiveRecord>();
    public DbSet<Domain.Modules.Dispatching.OnDutyMaterialRecord> OnDutyMaterialRecords => Set<Domain.Modules.Dispatching.OnDutyMaterialRecord>();
    public DbSet<Domain.Modules.Dispatching.VacancyResolutionRun> VacancyResolutionRuns => Set<Domain.Modules.Dispatching.VacancyResolutionRun>();

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

    // Infrastructure Module
    public DbSet<Domain.Modules.Infrastructure.WorkerSchedule> WorkerSchedules => Set<Domain.Modules.Infrastructure.WorkerSchedule>();
    public DbSet<Domain.Modules.Infrastructure.WorkerExecutionLog> WorkerExecutionLogs => Set<Domain.Modules.Infrastructure.WorkerExecutionLog>();
    public DbSet<Domain.Modules.Infrastructure.ProcessingLock> ProcessingLocks => Set<Domain.Modules.Infrastructure.ProcessingLock>();

    // Payroll Module
    public DbSet<Domain.Modules.Payroll.EarningCodeRule> EarningCodeRules => Set<Domain.Modules.Payroll.EarningCodeRule>();
    public DbSet<Domain.Modules.Payroll.PayRate> PayRates => Set<Domain.Modules.Payroll.PayRate>();
    public DbSet<Domain.Modules.Payroll.EarningApproval> EarningApprovals => Set<Domain.Modules.Payroll.EarningApproval>();

    // Notifications Module
    public DbSet<Domain.Modules.Notifications.NotificationRequest> NotificationRequests => Set<Domain.Modules.Notifications.NotificationRequest>();
    public DbSet<Domain.Modules.Notifications.NotificationResponse> NotificationResponses => Set<Domain.Modules.Notifications.NotificationResponse>();
    public DbSet<Domain.Modules.Notifications.NotificationProviderConfig> NotificationProviderConfigs => Set<Domain.Modules.Notifications.NotificationProviderConfig>();

    // AbsenceVacancy Module
    public DbSet<Domain.Modules.AbsenceVacancy.AbsenceCode> AbsenceCodes => Set<Domain.Modules.AbsenceVacancy.AbsenceCode>();
    public DbSet<Domain.Modules.AbsenceVacancy.AbsenceCodeCraftOverride> AbsenceCodeCraftOverrides => Set<Domain.Modules.AbsenceVacancy.AbsenceCodeCraftOverride>();
    public DbSet<Domain.Modules.AbsenceVacancy.AbsenceApproval> AbsenceApprovals => Set<Domain.Modules.AbsenceVacancy.AbsenceApproval>();
    public DbSet<Domain.Modules.AbsenceVacancy.AbsenceMarkUp> AbsenceMarkUps => Set<Domain.Modules.AbsenceVacancy.AbsenceMarkUp>();
    public DbSet<Domain.Modules.AbsenceVacancy.CompensationBalance> CompensationBalances => Set<Domain.Modules.AbsenceVacancy.CompensationBalance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrewServiceDbContext).Assembly);

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

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();

        return base.SaveChangesAsync(cancellationToken);
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
