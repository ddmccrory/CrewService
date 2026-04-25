using CrewService.Domain.Models.Parents;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class FraCertificationConfigConfiguration : IEntityTypeConfiguration<FraCertificationConfig>
{
    public void Configure(EntityTypeBuilder<FraCertificationConfig> builder)
    {
        builder.HasKey(c => c.CtrlNbr);
        builder.Property(c => c.CtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.ParentCtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.RailroadCtrlNbr).HasConversion(x => x!.Value, v => ControlNumber.Create(v));

        builder.HasOne<Parent>().WithMany().HasForeignKey(c => c.ParentCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(c => c.RailroadCtrlNbr).OnDelete(DeleteBehavior.Cascade).IsRequired(false);

        builder.OwnsOne(c => c.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class FraCertificationCheckConfigConfiguration : IEntityTypeConfiguration<FraCertificationCheckConfig>
{
    public void Configure(EntityTypeBuilder<FraCertificationCheckConfig> builder)
    {
        builder.HasKey(c => c.CtrlNbr);
        builder.Property(c => c.CtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.ParentCtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.RailroadCtrlNbr).HasConversion(x => x!.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.CheckType).HasMaxLength(50).IsRequired();

        builder.HasOne<Parent>().WithMany().HasForeignKey(c => c.ParentCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(c => c.RailroadCtrlNbr).OnDelete(DeleteBehavior.Cascade).IsRequired(false);

        builder.OwnsOne(c => c.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

