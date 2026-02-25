using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Modules.Payroll;

internal class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.HasKey(t => t.CtrlNbr);
        builder.Property(t => t.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(t => t.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(t => t.DateUtc).IsRequired();
        builder.Property(t => t.EntryType).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Hours).HasPrecision(8, 2).IsRequired();
        builder.Property(t => t.ReasonCode).HasMaxLength(50);
        builder.Property(t => t.Notes).HasMaxLength(1000);
        builder.Property(t => t.IsAdjustment).IsRequired();
        builder.Property(t => t.OriginalEntryCtrlNbr).HasConversion(c => c == null ? (long?)null : c.Value, v => v.HasValue ? ControlNumber.Create(v.Value) : null);
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
        builder.Property(r => r.PayrollRunCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(r => r.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(r => r.EarningsType).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Amount).HasPrecision(12, 2).IsRequired();
        builder.Property(r => r.Hours).HasPrecision(8, 2).IsRequired();
        builder.Property(r => r.PolicyRef).HasMaxLength(100);
        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
