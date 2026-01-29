using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.CtrlNbr);

        builder.Property(e => e.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(e => e.ClientCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(e => e.EmploymentStatusCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(e => e.EmployeeNumber).HasMaxLength(4).IsRequired();
        builder.Property(e => e.SocialSecurityNumber).HasMaxLength(9).IsRequired();
        builder.Property(e => e.DriversLicenseNumber).HasMaxLength(50);
        builder.Property(e => e.IssuingState).HasMaxLength(2);
        builder.Property(e => e.Gender).HasMaxLength(1).IsRequired();
        builder.Property(e => e.Race).HasMaxLength(100).IsRequired();
        builder.Property(e => e.UserId).HasMaxLength(128).IsRequired();

        builder.HasIndex(e => e.EmployeeNumber).IsUnique();
        builder.HasIndex(e => e.SocialSecurityNumber).IsUnique();

        builder.HasMany(e => e.Addresses)
            .WithOne()
            .HasForeignKey(a => a.EmployeeCtrlNbr)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.PhoneNumbers)
            .WithOne()
            .HasForeignKey(p => p.EmployeeCtrlNbr)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.EmailAddresses)
            .WithOne()
            .HasForeignKey(em => em.EmployeeCtrlNbr)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(e => e.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(e => e.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}