using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Modules.AbsenceVacancy;

internal class AbsenceRequestConfiguration : IEntityTypeConfiguration<AbsenceRequest>
{
    public void Configure(EntityTypeBuilder<AbsenceRequest> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(r => r.StartUtc).IsRequired();
        builder.Property(r => r.ReasonCode).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Status).HasMaxLength(20).IsRequired();
        builder.Property(r => r.ApprovedByCtrlNbr).HasConversion(c => c == null ? (long?)null : c.Value, v => v.HasValue ? ControlNumber.Create(v.Value) : null);
        builder.Property(r => r.Notes).HasMaxLength(1000);
        builder.Property(r => r.AbsenceCodeCtrlNbr).HasConversion(c => c == null ? (long?)null : c.Value, v => v.HasValue ? ControlNumber.Create(v.Value) : null);
        builder.Property(r => r.PositionSlotCtrlNbr).HasConversion(c => c == null ? (long?)null : c.Value, v => v.HasValue ? ControlNumber.Create(v.Value) : null);

        builder.HasMany(r => r.Approvals).WithOne().HasForeignKey(a => a.AbsenceRequestCtrlNbr);
        builder.HasMany(r => r.MarkUps).WithOne().HasForeignKey(m => m.AbsenceRequestCtrlNbr);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class VacancyImpactConfiguration : IEntityTypeConfiguration<VacancyImpact>
{
    public void Configure(EntityTypeBuilder<VacancyImpact> builder)
    {
        builder.HasKey(v => v.CtrlNbr);
        builder.Property(v => v.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(v => v.AbsenceRequestCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(v => v.PositionSlotCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(v => v.ImpactStartUtc).IsRequired();
        builder.OwnsOne(v => v.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(v => v.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(v => v.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class AbsenceApprovalConfiguration : IEntityTypeConfiguration<AbsenceApproval>
{
    public void Configure(EntityTypeBuilder<AbsenceApproval> builder)
    {
        builder.HasKey(a => a.CtrlNbr);
        builder.Property(a => a.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.AbsenceRequestCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.ApprovalOfficerCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.Status).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(500);
        builder.OwnsOne(a => a.CreatedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.ModifiedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.DeletedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class AbsenceMarkUpConfiguration : IEntityTypeConfiguration<AbsenceMarkUp>
{
    public void Configure(EntityTypeBuilder<AbsenceMarkUp> builder)
    {
        builder.HasKey(m => m.CtrlNbr);
        builder.Property(m => m.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(m => m.AbsenceRequestCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.OwnsOne(m => m.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(m => m.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(m => m.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
