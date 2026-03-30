using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class AbolishmentRecordConfiguration : IEntityTypeConfiguration<AbolishmentRecord>
{
    public void Configure(EntityTypeBuilder<AbolishmentRecord> builder)
    {
        builder.HasKey(a => a.CtrlNbr);
        builder.Property(a => a.CtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.TargetCtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.AbolishmentType).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(500).IsRequired();

        builder.OwnsOne(a => a.CreatedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.ModifiedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.DeletedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}