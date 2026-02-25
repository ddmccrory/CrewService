using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Modules.TenantConfig;

internal class RailroadGroupPlacementConfiguration : IEntityTypeConfiguration<RailroadGroupPlacement>
{
    public void Configure(EntityTypeBuilder<RailroadGroupPlacement> builder)
    {
        builder.HasKey(p => p.CtrlNbr);

        builder.Property(p => p.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(p => p.RailroadCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value)).IsRequired();

        builder.Property(p => p.GroupCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value)).IsRequired();

        builder.HasIndex(p => new { p.RailroadCtrlNbr, p.GroupCtrlNbr }).IsUnique();
        builder.HasIndex(p => p.GroupCtrlNbr);
        builder.HasIndex(p => p.RailroadCtrlNbr);

        builder.OwnsOne(p => p.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(p => p.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(p => p.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}
