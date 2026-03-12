using CrewService.Domain.Modules.Infrastructure;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class WorkerScheduleConfiguration : IEntityTypeConfiguration<WorkerSchedule>
{
    public void Configure(EntityTypeBuilder<WorkerSchedule> builder)
    {
        builder.HasKey(w => w.CtrlNbr);
        builder.Property(w => w.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(w => w.WorkAreaGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(w => w.WorkerType).HasMaxLength(50).IsRequired();
        builder.Property(w => w.CronExpression).HasMaxLength(100);
        builder.Property(w => w.LastRunStatus).HasMaxLength(20);

        builder.OwnsOne(w => w.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(w => w.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(w => w.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class WorkerExecutionLogConfiguration : IEntityTypeConfiguration<WorkerExecutionLog>
{
    public void Configure(EntityTypeBuilder<WorkerExecutionLog> builder)
    {
        builder.HasKey(l => l.CtrlNbr);
        builder.Property(l => l.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(l => l.WorkerScheduleCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(l => l.Status).HasMaxLength(20).IsRequired();
        builder.Property(l => l.ErrorMessage).HasMaxLength(2000);

        builder.OwnsOne(l => l.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(l => l.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(l => l.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class ProcessingLockConfiguration : IEntityTypeConfiguration<ProcessingLock>
{
    public void Configure(EntityTypeBuilder<ProcessingLock> builder)
    {
        builder.HasKey(p => p.LockKey);
        builder.Property(p => p.LockKey).HasMaxLength(200);
        builder.Property(p => p.AcquiredByInstance).HasMaxLength(100).IsRequired();
    }
}
