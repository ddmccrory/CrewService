using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Outbox;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Data;

internal sealed class CrewServiceDbContext(
    DbContextOptions<CrewServiceDbContext> options,
    ICurrentUserService currentUserService) : DbContext(options)
{
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
    public DbSet<RailroadEmployee> RailroadEmployees => Set<RailroadEmployee>();
    public DbSet<RailroadPool> RailroadPools => Set<RailroadPool>();
    public DbSet<RailroadPoolEmployee> RailroadPoolEmployees => Set<RailroadPoolEmployee>();
    public DbSet<RailroadPoolPayrollTier> RailroadPoolPayrollTiers => Set<RailroadPoolPayrollTier>();
    public DbSet<Roster> Rosters => Set<Roster>();
    public DbSet<Seniority> Seniority => Set<Seniority>();
    public DbSet<SeniorityState> SeniorityStates => Set<SeniorityState>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrewServiceDbContext).Assembly);

        //modelBuilder.HasDefaultSchema("crew_assignment");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();

        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditableEntities()
    {
        string auditName = currentUserService.GetUserName();

        if (string.IsNullOrWhiteSpace(auditName))
            throw new InvalidOperationException("Audit name cannot be null or empty. Ensure user context is available.");

        foreach (var entry in ChangeTracker.Entries<Entity>())
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
