using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class EmployeeNotificationConfiguration : IEntityTypeConfiguration<EmployeeNotification>
{
    public void Configure(EntityTypeBuilder<EmployeeNotification> builder)
    {
        builder.HasKey(n => n.CtrlNbr);
        builder.Property(n => n.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(n => n.RailroadCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(n => n.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(n => n.Category).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.Audience).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(n => n.RequiresAcknowledgement).IsRequired();
        builder.Property(n => n.IncludeInHistory).IsRequired();
        builder.Property(n => n.CreatedAtUtc).IsRequired();

        builder.OwnsOne(n => n.Subject, s =>
        {
            s.Property(x => x.SubjectType).HasColumnName("SubjectType").HasMaxLength(50);
            s.Property(x => x.SubjectCtrlNbr).HasColumnName("SubjectCtrlNbr")
                .HasConversion(c => c.Value, v => ControlNumber.Create(v));
        });

        builder.HasMany(n => n.Acknowledgements).WithOne()
            .HasForeignKey(a => a.EmployeeNotificationCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(n => n.RailroadCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(n => n.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(n => n.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(n => n.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(n => n.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class NotificationAcknowledgementConfiguration : IEntityTypeConfiguration<NotificationAcknowledgement>
{
    public void Configure(EntityTypeBuilder<NotificationAcknowledgement> builder)
    {
        builder.HasKey(a => a.CtrlNbr);
        builder.Property(a => a.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.EmployeeNotificationCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.Method).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Confirmed).IsRequired();
        builder.Property(a => a.NotifiedAtUtc).IsRequired();
        builder.Property(a => a.PhoneNumber).HasMaxLength(30);
        builder.Property(a => a.Notes).HasMaxLength(256);

        builder.OwnsOne(a => a.CreatedBy, x => { x.Property(p => p.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.ModifiedBy, x => { x.Property(p => p.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.DeletedBy, x => { x.Property(p => p.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
