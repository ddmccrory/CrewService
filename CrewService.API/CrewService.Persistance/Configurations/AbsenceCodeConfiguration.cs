using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class AbsenceCodeConfiguration : IEntityTypeConfiguration<AbsenceCode>
{
    public void Configure(EntityTypeBuilder<AbsenceCode> builder)
    {
        builder.HasKey(a => a.CtrlNbr);
        builder.Property(a => a.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.RailroadCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(a => a.Code).HasMaxLength(10).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(100).IsRequired();
        builder.Property(a => a.DefaultAutoMarkUpHours).HasPrecision(6, 2);
        builder.HasIndex(a => new { a.RailroadCtrlNbr, a.Code }).IsUnique();
        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(a => a.RailroadCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(a => a.CreatedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.ModifiedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.DeletedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class AbsenceCodeCraftOverrideConfiguration : IEntityTypeConfiguration<AbsenceCodeCraftOverride>
{
    public void Configure(EntityTypeBuilder<AbsenceCodeCraftOverride> builder)
    {
        builder.HasKey(a => a.CtrlNbr);
        builder.Property(a => a.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.AbsenceCodeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.OverrideAutoMarkUpHours).HasPrecision(6, 2);
        builder.HasIndex(a => new { a.AbsenceCodeCtrlNbr, a.CraftCtrlNbr }).IsUnique();

        builder.HasOne<AbsenceCode>().WithMany().HasForeignKey(a => a.AbsenceCodeCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Craft>().WithMany().HasForeignKey(a => a.CraftCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(a => a.CreatedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.ModifiedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.DeletedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
