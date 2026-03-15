using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class GroupTypeConfiguration : IEntityTypeConfiguration<GroupType>
{
    public void Configure(EntityTypeBuilder<GroupType> builder)
    {
        builder.HasKey(g => g.CtrlNbr);
        builder.Property(g => g.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(500);

        builder.OwnsOne(g => g.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(g => g.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(g => g.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class DynamicGroupConfiguration : IEntityTypeConfiguration<DynamicGroup>
{
    public void Configure(EntityTypeBuilder<DynamicGroup> builder)
    {
        builder.HasKey(g => g.CtrlNbr);
        builder.Property(g => g.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(g => g.GroupTypeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(g => g.ParentGroupCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Path).HasMaxLength(500);

        builder.HasOne<GroupType>().WithMany().HasForeignKey(g => g.GroupTypeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(g => g.ParentGroupCtrlNbr).OnDelete(DeleteBehavior.SetNull);

        builder.OwnsOne(g => g.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(g => g.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(g => g.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class GroupAttributeDefinitionConfiguration : IEntityTypeConfiguration<GroupAttributeDefinition>
{
    public void Configure(EntityTypeBuilder<GroupAttributeDefinition> builder)
    {
        builder.HasKey(d => d.CtrlNbr);
        builder.Property(d => d.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(d => d.GroupTypeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(d => d.AttributeName).HasMaxLength(100).IsRequired();
        builder.Property(d => d.DataType).HasMaxLength(30).IsRequired();
        builder.Property(d => d.DefaultValue).HasMaxLength(500);

        builder.HasOne<GroupType>().WithMany().HasForeignKey(d => d.GroupTypeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(d => d.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(d => d.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(d => d.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class GroupAttributeValueConfiguration : IEntityTypeConfiguration<GroupAttributeValue>
{
    public void Configure(EntityTypeBuilder<GroupAttributeValue> builder)
    {
        builder.HasKey(v => v.CtrlNbr);
        builder.Property(v => v.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(v => v.GroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(v => v.AttributeDefinitionCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(v => v.Value).HasMaxLength(1000);

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(v => v.GroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<GroupAttributeDefinition>().WithMany().HasForeignKey(v => v.AttributeDefinitionCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(v => v.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(v => v.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(v => v.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class RailroadGroupPlacementConfiguration : IEntityTypeConfiguration<RailroadGroupPlacement>
{
    public void Configure(EntityTypeBuilder<RailroadGroupPlacement> builder)
    {
        builder.HasKey(p => p.CtrlNbr);
        builder.Property(p => p.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.RailroadCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.GroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));

        builder.HasOne<Railroad>().WithMany().HasForeignKey(p => p.RailroadCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(p => p.GroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(p => p.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
