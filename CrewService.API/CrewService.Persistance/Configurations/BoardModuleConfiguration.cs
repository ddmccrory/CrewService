using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class BoardCascadePolicyConfiguration : IEntityTypeConfiguration<BoardCascadePolicy>
{
    public void Configure(EntityTypeBuilder<BoardCascadePolicy> builder)
    {
        builder.HasKey(p => p.CtrlNbr);
        builder.Property(p => p.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.WorkAreaGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.CascadeMode).HasMaxLength(30).IsRequired();
        builder.Property(p => p.SelectionStrategy).HasMaxLength(50);

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(p => p.WorkAreaGroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Craft>().WithMany().HasForeignKey(p => p.CraftCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(p => p.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class RequiredPositionsStrategyConfiguration : IEntityTypeConfiguration<RequiredPositionsStrategy>
{
    public void Configure(EntityTypeBuilder<RequiredPositionsStrategy> builder)
    {
        builder.HasKey(s => s.CtrlNbr);
        builder.Property(s => s.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.Code).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.FormulaType).HasMaxLength(50).IsRequired();
        builder.Property(s => s.ParametersJson).HasMaxLength(2000).HasDefaultValue("{}");

        builder.HasIndex(s => s.Code).IsUnique();

        builder.OwnsOne(s => s.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class CraftRequiredPositionsStrategyConfiguration : IEntityTypeConfiguration<CraftRequiredPositionsStrategy>
{
    public void Configure(EntityTypeBuilder<CraftRequiredPositionsStrategy> builder)
    {
        builder.HasKey(cs => cs.CtrlNbr);
        builder.Property(cs => cs.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(cs => cs.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(cs => cs.StrategyCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(cs => cs.ParametersJson).HasMaxLength(2000).IsRequired(false);

        builder.HasIndex(cs => cs.CraftCtrlNbr).IsUnique();

        builder.HasOne<Craft>().WithMany().HasForeignKey(cs => cs.CraftCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RequiredPositionsStrategy>().WithMany()
            .HasForeignKey(cs => cs.StrategyCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(cs => cs.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(cs => cs.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(cs => cs.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}