using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class DailyEmployeeStatusRecordConfiguration : IEntityTypeConfiguration<DailyEmployeeStatusRecord>
{
    public void Configure(EntityTypeBuilder<DailyEmployeeStatusRecord> builder)
    {
        builder.HasKey(d => d.CtrlNbr);
        builder.Property(d => d.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(d => d.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(d => d.WorkAreaGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(d => d.StatusCode).HasMaxLength(30).IsRequired();
        builder.Property(d => d.SnapshotJson).HasMaxLength(4000);
        builder.HasIndex(d => new { d.EmployeeCtrlNbr, d.RecordDate });

        builder.OwnsOne(d => d.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(d => d.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(d => d.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
