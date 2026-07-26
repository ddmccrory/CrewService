using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class NotificationProviderConfigConfiguration : IEntityTypeConfiguration<NotificationProviderConfig>
{
    public void Configure(EntityTypeBuilder<NotificationProviderConfig> builder)
    {
        builder.HasKey(c => c.CtrlNbr);
        builder.Property(c => c.CtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.WorkAreaGroupCtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.ProviderType).HasMaxLength(20).IsRequired();
        builder.Property(c => c.ConfigJson).HasMaxLength(4000).IsRequired();

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(c => c.WorkAreaGroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(c => c.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }

internal class NotificationTypeConfigConfiguration : IEntityTypeConfiguration<NotificationTypeConfig>
{
    public void Configure(EntityTypeBuilder<NotificationTypeConfig> builder)
    {
        builder.HasKey(c => c.CtrlNbr);
        builder.Property(c => c.CtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.RailroadCtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.Key).HasMaxLength(80).IsRequired();
        builder.Property(c => c.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(c => c.IsEnabled).IsRequired();
        builder.Property(c => c.RequiresAcknowledgementDefault).IsRequired();
        builder.Property(c => c.Audience).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.SendInApp).IsRequired();
        builder.Property(c => c.SendEmail).IsRequired();
        builder.Property(c => c.SendText).IsRequired();
        builder.Property(c => c.SendExternalApi).IsRequired();
        builder.Property(c => c.MessageTemplate).HasMaxLength(2000).IsRequired();

        builder.HasIndex(c => new { c.RailroadCtrlNbr, c.Key }).IsUnique();

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(c => c.RailroadCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(c => c.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
}
