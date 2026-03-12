using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class EmployeeCertificationConfiguration : IEntityTypeConfiguration<EmployeeCertification>
{
    public void Configure(EntityTypeBuilder<EmployeeCertification> builder)
    {
        builder.HasKey(e => e.CtrlNbr);
        builder.Property(e => e.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(e => e.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(e => e.RegulatoryQualificationCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(e => e.CertificationType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.CertificationNumber).HasMaxLength(100);
        builder.Property(e => e.SuspensionReason).HasMaxLength(500);

        builder.HasMany(e => e.EligibilityChecks).WithOne().HasForeignKey(c => c.EmployeeCertificationCtrlNbr);

        builder.OwnsOne(e => e.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(e => e.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(e => e.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class CertificationEligibilityCheckConfiguration : IEntityTypeConfiguration<CertificationEligibilityCheck>
{
    public void Configure(EntityTypeBuilder<CertificationEligibilityCheck> builder)
    {
        builder.HasKey(c => c.CtrlNbr);
        builder.Property(c => c.CtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.EmployeeCertificationCtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.CheckType).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Result).HasMaxLength(20).IsRequired();
        builder.Property(c => c.EvaluatorName).HasMaxLength(100);

        builder.OwnsOne(c => c.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
