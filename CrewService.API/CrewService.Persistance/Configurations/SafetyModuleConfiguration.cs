using CrewService.Domain.Modules.Safety;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class SafetyObservationConfiguration : IEntityTypeConfiguration<SafetyObservation>
{
    public void Configure(EntityTypeBuilder<SafetyObservation> builder)
    {
        builder.HasKey(o => o.CtrlNbr);
        builder.Property(o => o.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(o => o.WorkAreaGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(o => o.ObserverEmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(o => o.CategoryCode).HasMaxLength(50).IsRequired();
        builder.Property(o => o.AreaCode).HasMaxLength(50).IsRequired();
        builder.Property(o => o.SubdivisionCode).HasMaxLength(50);
        builder.Property(o => o.Description).HasMaxLength(2000).IsRequired();
        builder.Property(o => o.Status).HasMaxLength(20).IsRequired();

        builder.HasMany(o => o.Actions).WithOne().HasForeignKey(a => a.ObservationCtrlNbr);

        builder.OwnsOne(o => o.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(o => o.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(o => o.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class SafetyObservationActionConfiguration : IEntityTypeConfiguration<SafetyObservationAction>
{
    public void Configure(EntityTypeBuilder<SafetyObservationAction> builder)
    {
        builder.HasKey(a => a.CtrlNbr);
        builder.Property(a => a.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.ObservationCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.TakenByCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.ActionDescription).HasMaxLength(2000).IsRequired();

        builder.OwnsOne(a => a.CreatedBy, ab => { ab.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.ModifiedBy, ab => { ab.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.DeletedBy, ab => { ab.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
