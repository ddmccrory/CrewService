using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Modules.TenantConfig;

internal class GroupAttributeDefinitionConfiguration : IEntityTypeConfiguration<GroupAttributeDefinition>
{
    public void Configure(EntityTypeBuilder<GroupAttributeDefinition> builder)
    {
        builder.HasKey(a => a.CtrlNbr);

        builder.Property(a => a.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(a => a.GroupTypeCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value)).IsRequired();

        builder.Property(a => a.AttributeName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.DataType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.IsRequired).IsRequired();
        builder.Property(a => a.DefaultValue).HasMaxLength(500);

        builder.OwnsOne(a => a.CreatedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value, value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(a => a.ModifiedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value, value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(a => a.DeletedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value, value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });
    }
}

internal class GroupAttributeValueConfiguration : IEntityTypeConfiguration<GroupAttributeValue>
{
    public void Configure(EntityTypeBuilder<GroupAttributeValue> builder)
    {
        builder.HasKey(v => v.CtrlNbr);

        builder.Property(v => v.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(v => v.GroupCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value)).IsRequired();

        builder.Property(v => v.AttributeDefinitionCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value)).IsRequired();

        builder.Property(v => v.Value).HasMaxLength(2000);

        builder.OwnsOne(v => v.CreatedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value, value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(v => v.ModifiedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value, value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(v => v.DeletedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value, value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });
    }
}
