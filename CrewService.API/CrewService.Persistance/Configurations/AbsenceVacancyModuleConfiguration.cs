using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class AbsenceRequestConfiguration : IEntityTypeConfiguration<AbsenceRequest>
{
    public void Configure(EntityTypeBuilder<AbsenceRequest> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.AbsenceCodeCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(r => r.ApprovedByCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(r => r.ScheduledStartUtc).IsRequired();
        builder.Property(r => r.ReasonCode).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Status).HasMaxLength(20).IsRequired();
        builder.Property(r => r.AutoMarkOffOnApproval).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(1000);

        builder.HasMany(r => r.Approvals).WithOne().HasForeignKey("AbsenceRequestCtrlNbr").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.MarkUps).WithOne().HasForeignKey("AbsenceRequestCtrlNbr").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AbsenceCode>().WithMany().HasForeignKey(r => r.AbsenceCodeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.ApprovedByCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class VacancyImpactConfiguration : IEntityTypeConfiguration<VacancyImpact>
{
    public void Configure(EntityTypeBuilder<VacancyImpact> builder)
    {
        builder.HasKey(i => i.CtrlNbr);
        builder.Property(i => i.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(i => i.AbsenceRequestCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(i => i.PositionSlotCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));

        builder.HasOne<AbsenceRequest>().WithMany().HasForeignKey(i => i.AbsenceRequestCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PositionSlot>().WithMany().HasForeignKey(i => i.PositionSlotCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(i => i.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(i => i.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(i => i.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
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

        builder.HasOne<Employee>().WithMany().HasForeignKey(a => a.ApprovalOfficerCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(a => a.CreatedBy, ab => { ab.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.ModifiedBy, ab => { ab.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.DeletedBy, ab => { ab.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
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
