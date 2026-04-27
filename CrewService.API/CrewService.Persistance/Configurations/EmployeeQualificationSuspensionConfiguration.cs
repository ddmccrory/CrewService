using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal sealed class EmployeeQualificationSuspensionConfiguration
    : IEntityTypeConfiguration<EmployeeQualificationSuspension>
{
    public void Configure(EntityTypeBuilder<EmployeeQualificationSuspension> builder)
    {
        builder.HasKey(s => s.CtrlNbr);

        builder.Property(s => s.CtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));

        builder.Property(s => s.EmployeeCtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));

        builder.Property(s => s.QualificationTypeCtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));

        builder.Property(s => s.SuspendedBy).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Reason).HasMaxLength(500).IsRequired();
        builder.Property(s => s.ReinstatedBy).HasMaxLength(50);
        builder.Property(s => s.ReinstatementNote).HasMaxLength(500);

        builder.HasIndex(s => new { s.EmployeeCtrlNbr, s.QualificationTypeCtrlNbr });

        builder.HasOne<Employee>().WithMany()
            .HasForeignKey(s => s.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<QualificationType>().WithMany()
            .HasForeignKey(s => s.QualificationTypeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(s => s.IsActive);

        builder.OwnsOne(s => s.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(s => s.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(s => s.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}
