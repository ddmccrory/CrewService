using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class ChangeNotificationConfiguration : IEntityTypeConfiguration<ChangeNotification>
{
    public void Configure(EntityTypeBuilder<ChangeNotification> builder)
    {
        builder.HasKey(c => c.CtrlNbr);

        builder.Property(c => c.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(c => c.WorkAreaGroupCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(c => c.ChangeType).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500).IsRequired();
        builder.Property(c => c.Status).HasMaxLength(20).IsRequired();
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.OwnsOne(c => c.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value, value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(c => c.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value, value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(c => c.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value, value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}
