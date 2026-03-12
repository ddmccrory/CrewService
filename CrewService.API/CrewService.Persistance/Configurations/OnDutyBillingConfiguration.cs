using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class OnDutyBillingRecordConfiguration : IEntityTypeConfiguration<OnDutyBillingRecord>
{
    public void Configure(EntityTypeBuilder<OnDutyBillingRecord> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.OnDutyRecordCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.BillingType).HasMaxLength(20).IsRequired();
        builder.Property(r => r.BillingCode).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Amount).HasPrecision(10, 2);
        builder.Property(r => r.Hours).HasPrecision(5, 2);
        builder.Property(r => r.Description).HasMaxLength(200);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class OnDutyLocomotiveRecordConfiguration : IEntityTypeConfiguration<OnDutyLocomotiveRecord>
{
    public void Configure(EntityTypeBuilder<OnDutyLocomotiveRecord> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.OnDutyRecordCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.LocomotiveNumber).HasMaxLength(20).IsRequired();
        builder.Property(r => r.LocomotiveTypeCode).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Hours).HasPrecision(5, 2);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class OnDutyMaterialRecordConfiguration : IEntityTypeConfiguration<OnDutyMaterialRecord>
{
    public void Configure(EntityTypeBuilder<OnDutyMaterialRecord> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.OnDutyRecordCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.MaterialCode).HasMaxLength(50).IsRequired();
        builder.Property(r => r.CategoryCode).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Quantity).HasPrecision(10, 2);
        builder.Property(r => r.UnitCost).HasPrecision(10, 2);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
