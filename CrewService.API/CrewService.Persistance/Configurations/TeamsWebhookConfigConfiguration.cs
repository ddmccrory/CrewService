using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class TeamsWebhookConfigConfiguration : IEntityTypeConfiguration<TeamsWebhookConfig>
{
    public void Configure(EntityTypeBuilder<TeamsWebhookConfig> builder)
    {
        builder.HasKey(t => t.CtrlNbr);

        builder.Property(t => t.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(t => t.RailroadCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(t => t.WorkAreaGroupCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));

        builder.Property(t => t.Channel)
            .HasConversion(
                c => c.ToString(),
                c => Enum.Parse<NotificationChannel>(c))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.WebhookUrl).HasMaxLength(500).IsRequired();
        builder.Property(t => t.IsEnabled).IsRequired();

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(t => t.RailroadCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(t => t.WorkAreaGroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(t => t.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(t => t.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(t => t.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}
