using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class PositionChangeRecordConfiguration : IEntityTypeConfiguration<PositionChangeRecord>
{
    public void Configure(EntityTypeBuilder<PositionChangeRecord> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));

        builder.OwnsOne(r => r.CreatedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
        builder.OwnsOne(r => r.ModifiedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
        builder.OwnsOne(r => r.DeletedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });

        builder.Property(r => r.RailroadCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.EmployeeNotificationCtrlNbr)
            .HasConversion(c => c == null ? (long?)null : c.Value, v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(r => r.SourceType).HasMaxLength(64).IsRequired();
        builder.Property(r => r.SourceCtrlNbr)
            .HasConversion(c => c == null ? (long?)null : c.Value, v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(r => r.ChangeType).HasMaxLength(64).IsRequired();
        builder.Property(r => r.Message).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.EffectiveAtUtc);
        builder.Property(r => r.RequiresAcknowledgement).IsRequired();
        builder.Property(r => r.IsOpen).IsRequired();
        builder.Property(r => r.OpenedAtUtc).IsRequired();
        builder.Property(r => r.ClosedAtUtc);
        builder.Property(r => r.ClosedReason).HasMaxLength(200);

        builder.HasIndex(r => new { r.EmployeeCtrlNbr, r.IsOpen });
        builder.HasIndex(r => new { r.RailroadCtrlNbr, r.IsOpen });
        builder.HasIndex(r => new { r.SourceType, r.SourceCtrlNbr, r.IsOpen });
        builder.HasIndex(r => r.EmployeeNotificationCtrlNbr);

        builder.HasOne<EmployeeNotification>()
            .WithMany()
            .HasForeignKey(r => r.EmployeeNotificationCtrlNbr)
            .OnDelete(DeleteBehavior.SetNull);
    }
}