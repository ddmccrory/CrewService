using CrewService.Infrastructure.Models.UserAccount;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Data;

internal sealed class UserAccessDbContext(DbContextOptions<UserAccessDbContext> options)
    : IdentityDbContext<User>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.FullName)
            .HasDatabaseName("FullNameIndex");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.FullNameLNF)
            .HasDatabaseName("FullNameIndexLNF");

        modelBuilder.Entity<User>()
            .ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_AspNetUsers_EmployeeName_Required",
                    "[EmployeeNumber] IS NULL OR ([FirstName] IS NOT NULL AND [FirstName] <> '' AND [LastName] IS NOT NULL AND [LastName] <> '')");
            });
    }
}
