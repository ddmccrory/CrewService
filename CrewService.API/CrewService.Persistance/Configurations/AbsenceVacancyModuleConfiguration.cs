using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Seniority;
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
        builder.Property(r => r.DeniedByCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(r => r.CancelledByCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(r => r.ScheduledStartUtc).IsRequired();
        builder.Property(r => r.ScheduledEndUtc);
        builder.Property(r => r.ApprovedAtUtc);
        builder.Property(r => r.DeniedAtUtc);
        builder.Property(r => r.CancelledAtUtc);
        builder.Property(r => r.ReasonCode).HasMaxLength(50).IsRequired();
        builder.Property(r => r.AutoMarkOffOnApproval).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(1000);

        builder.HasMany(r => r.StartRecords).WithOne().HasForeignKey("AbsenceRequestCtrlNbr").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.EndRecords).WithOne().HasForeignKey("AbsenceRequestCtrlNbr").OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AbsenceCode>().WithMany().HasForeignKey(r => r.AbsenceCodeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.ApprovedByCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.DeniedByCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.CancelledByCtrlNbr).OnDelete(DeleteBehavior.Restrict);
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

internal class AbsenceStartRecordConfiguration : IEntityTypeConfiguration<AbsenceStartRecord>
{
    public void Configure(EntityTypeBuilder<AbsenceStartRecord> builder)
    {
        builder.HasKey(s => s.CtrlNbr);
        builder.Property(s => s.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.AbsenceRequestCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.HasIndex(s => s.AbsenceRequestCtrlNbr).IsUnique();

        builder.ToTable("AbsenceStartRecords");

        builder.OwnsOne(s => s.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class AbsenceEndRecordConfiguration : IEntityTypeConfiguration<AbsenceEndRecord>
{
    public void Configure(EntityTypeBuilder<AbsenceEndRecord> builder)
    {
        builder.HasKey(e => e.CtrlNbr);
        builder.Property(e => e.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(e => e.AbsenceRequestCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.HasIndex(e => e.AbsenceRequestCtrlNbr).IsUnique();

        builder.ToTable("AbsenceEndRecords");

        builder.OwnsOne(e => e.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(e => e.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(e => e.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class AbsenceRequestWaitListRecordConfiguration : IEntityTypeConfiguration<AbsenceRequestWaitListRecord>
{
    public void Configure(EntityTypeBuilder<AbsenceRequestWaitListRecord> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.AbsenceCodeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.CraftCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(r => r.DepartmentCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));

        builder.Property(r => r.WaitListType).HasMaxLength(64).IsRequired();
        builder.Property(r => r.RequestDateUtc).IsRequired();
        builder.Property(r => r.EntryUtc).IsRequired();
        builder.Property(r => r.AssignedAtUtc);
        builder.Property(r => r.AssignmentNotes).HasMaxLength(1000);

        builder.HasIndex(r => new { r.WaitListType, r.RequestDateUtc, r.AssignedAtUtc, r.EntryUtc });

        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AbsenceCode>().WithMany().HasForeignKey(r => r.AbsenceCodeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Craft>().WithMany().HasForeignKey(r => r.CraftCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(r => r.DepartmentCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class AbsenceRequestWaitListLinkConfiguration : IEntityTypeConfiguration<AbsenceRequestWaitListLink>
{
    public void Configure(EntityTypeBuilder<AbsenceRequestWaitListLink> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.AbsenceRequestCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.AbsenceRequestWaitListRecordCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));

        builder.HasIndex(r => new { r.AbsenceRequestCtrlNbr, r.AbsenceRequestWaitListRecordCtrlNbr }).IsUnique();

        builder.HasOne<AbsenceRequest>().WithMany().HasForeignKey(r => r.AbsenceRequestCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AbsenceRequestWaitListRecord>().WithMany().HasForeignKey(r => r.AbsenceRequestWaitListRecordCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
