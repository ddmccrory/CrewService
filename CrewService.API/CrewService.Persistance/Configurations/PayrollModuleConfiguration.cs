using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.HasKey(t => t.CtrlNbr);
        builder.Property(t => t.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(t => t.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(t => t.OriginalEntryCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(t => t.DateUtc).IsRequired();
        builder.Property(t => t.EntryType).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Hours).HasPrecision(8, 2).IsRequired();
        builder.Property(t => t.ReasonCode).HasMaxLength(50);
        builder.Property(t => t.Notes).HasMaxLength(1000);
        builder.Property(t => t.IsAdjustment).IsRequired();

        builder.HasOne<Employee>().WithMany().HasForeignKey(t => t.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TimeEntry>().WithMany().HasForeignKey(t => t.OriginalEntryCtrlNbr).OnDelete(DeleteBehavior.SetNull);

        builder.OwnsOne(t => t.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(t => t.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(t => t.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.PayPeriod).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Status).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Version).IsRequired();
        builder.HasIndex(r => r.PayPeriod);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class PayrollRecordConfiguration : IEntityTypeConfiguration<PayrollRecord>
{
    public void Configure(EntityTypeBuilder<PayrollRecord> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.PayrollRunCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.OnDutyRecordCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(r => r.EarningsType).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Amount).HasPrecision(12, 2);
        builder.Property(r => r.Hours).HasPrecision(8, 2);
        builder.Property(r => r.PolicyRef).HasMaxLength(100);
        builder.Property(r => r.ResolvedEarningCode).HasMaxLength(20);

        builder.HasOne<PayrollRun>().WithMany().HasForeignKey(r => r.PayrollRunCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OnDutyRecord>().WithMany().HasForeignKey(r => r.OnDutyRecordCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class EarningApprovalConfiguration : IEntityTypeConfiguration<EarningApproval>
{
    public void Configure(EntityTypeBuilder<EarningApproval> builder)
    {
        builder.HasKey(a => a.CtrlNbr);
        builder.Property(a => a.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.PayrollRecordCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.OfficerCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.Status).HasMaxLength(20).IsRequired();

        builder.HasOne<PayrollRecord>().WithMany().HasForeignKey(a => a.PayrollRecordCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Employee>().WithMany().HasForeignKey(a => a.OfficerCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(a => a.CreatedBy, ab => { ab.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.ModifiedBy, ab => { ab.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.DeletedBy, ab => { ab.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class PayrollExportBatchConfiguration : IEntityTypeConfiguration<PayrollExportBatch>
{
    public void Configure(EntityTypeBuilder<PayrollExportBatch> builder)
    {
        builder.HasKey(b => b.CtrlNbr);
        builder.Property(b => b.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.PayrollRunCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.ExportFormat).HasMaxLength(20).IsRequired();
        builder.Property(b => b.FilePath).HasMaxLength(500);

        builder.HasOne<PayrollRun>().WithMany().HasForeignKey(b => b.PayrollRunCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(b => b.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(b => b.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(b => b.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class PayrollImportRecordConfiguration : IEntityTypeConfiguration<PayrollImportRecord>
{
    public void Configure(EntityTypeBuilder<PayrollImportRecord> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.PayrollRecordCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(r => r.SourceFile).HasMaxLength(500).IsRequired();
        builder.Property(r => r.PaidAmount).HasPrecision(12, 2);
        builder.Property(r => r.MatchStatus).HasMaxLength(20).IsRequired();

        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollRecord>().WithMany().HasForeignKey(r => r.PayrollRecordCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
