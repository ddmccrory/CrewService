using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Services;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Data;

internal sealed class CrewAssignmentDbContext(DbContextOptions<CrewAssignmentDbContext> options) : DbContext(options)
{
    private readonly CurrentUserService _currentUserService = new();

    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<Railroad> Railroads => Set<Railroad>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<PhoneNumber> PhoneNumbers => Set<PhoneNumber>();
    public DbSet<EmailAddress> EmailAddresses => Set<EmailAddress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrewAssignmentDbContext).Assembly);

        //modelBuilder.HasDefaultSchema("crew_assignment");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();

        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditableEntities()
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = AuditStamp.Create(_currentUserService.GetUserName());
                    entry.Entity.ModifiedBy = AuditStamp.Create(_currentUserService.GetUserName());
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedBy = AuditStamp.Create(_currentUserService.GetUserName());
                    break;
            }
        }
    }
}
