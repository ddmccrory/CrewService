using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Modules.TenantConfig;

internal class DynamicGroupConfiguration : IEntityTypeConfiguration<DynamicGroup>
{
    public void Configure(EntityTypeBuilder<DynamicGroup> builder)
    {
        builder.HasKey(g => g.CtrlNbr);

        builder.Property(g => g.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(g => g.GroupTypeCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value)).IsRequired();

        builder.Property(g => g.Name).HasMaxLength(200).IsRequired();

        builder.Property(g => g.ParentGroupCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value.HasValue ? ControlNumber.Create(value.Value) : null);

        builder.Property(g => g.Path).HasMaxLength(2000);
        builder.Property(g => g.IsWorkArea).IsRequired();

        builder.HasIndex(g => new { g.ParentGroupCtrlNbr, g.Name });

        builder.OwnsOne(g => g.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(g => g.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(g => g.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}
