using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class EmployeeQualificationConfiguration : IEntityTypeConfiguration<EmployeeQualification>
{
    public void Configure(EntityTypeBuilder<EmployeeQualification> builder)
    {
        builder.HasKey(eq => eq.CtrlNbr);

        builder.Property(eq => eq.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(eq => eq.EmployeeCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(eq => eq.QualificationTypeCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(eq => eq.Status).HasMaxLength(20).IsRequired();
        builder.Property(eq => eq.GrantedBy).HasMaxLength(50).IsRequired();
        builder.Property(eq => eq.RevocationReason).HasMaxLength(200);

        builder.HasIndex(eq => new { eq.EmployeeCtrlNbr, eq.QualificationTypeCtrlNbr });

        builder.HasOne<Employee>().WithMany().HasForeignKey(eq => eq.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<QualificationType>().WithMany().HasForeignKey(eq => eq.QualificationTypeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(eq => eq.Evidence)
            .WithOne()
            .HasForeignKey(e => e.EmployeeQualificationCtrlNbr)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(eq => eq.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(eq => eq.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(eq => eq.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}

internal class QualificationEvidenceConfiguration : IEntityTypeConfiguration<QualificationEvidence>
{
    public void Configure(EntityTypeBuilder<QualificationEvidence> builder)
    {
        builder.HasKey(e => e.CtrlNbr);

        builder.Property(e => e.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(e => e.EmployeeQualificationCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(e => e.PrerequisiteCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));

        builder.Property(e => e.EvidenceType).HasMaxLength(20).IsRequired();
        builder.Property(e => e.EvidenceValue).HasMaxLength(200).IsRequired();
        builder.Property(e => e.RecordedBy).HasMaxLength(50).IsRequired();

        builder.HasOne<QualificationPrerequisite>()
            .WithMany()
            .HasForeignKey(e => e.PrerequisiteCtrlNbr)
            .OnDelete(DeleteBehavior.Restrict);

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
