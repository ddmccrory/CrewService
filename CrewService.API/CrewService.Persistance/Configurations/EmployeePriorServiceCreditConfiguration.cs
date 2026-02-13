using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class EmployeePriorServiceCreditConfiguration : IEntityTypeConfiguration<EmployeePriorServiceCredit>
{
    public void Configure(EntityTypeBuilder<EmployeePriorServiceCredit> builder)
    {
        builder.HasKey(e => e.CtrlNbr);

        builder.Property(e => e.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(e => e.EmployeeCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(e => e.ServiceYears).IsRequired();
        builder.Property(e => e.ServiceMonths).IsRequired();
        builder.Property(e => e.ServiceDays).IsRequired();

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

        builder.OwnsOne(e => e.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}