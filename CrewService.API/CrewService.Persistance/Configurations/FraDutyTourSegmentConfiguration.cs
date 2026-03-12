using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class FraDutyTourSegmentConfiguration : IEntityTypeConfiguration<FraDutyTourSegment>
{
    public void Configure(EntityTypeBuilder<FraDutyTourSegment> builder)
    {
        builder.HasKey(s => s.CtrlNbr);

        builder.Property(s => s.CtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.DutyTourCtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.OnDutyRecordCtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));

        builder.Property(s => s.PositionDescription).HasMaxLength(200).IsRequired();
        builder.Property(s => s.StartLocationCode).HasMaxLength(20).IsRequired();
        builder.Property(s => s.EndLocationCode).HasMaxLength(20);

        builder.OwnsOne(s => s.CreatedBy, a =>
        {
            a.Property(x => x.AuditName).HasConversion(
                n => n.Value, v => Name.Create(v)).HasMaxLength(50);
        });
        builder.OwnsOne(s => s.ModifiedBy, a =>
        {
            a.Property(x => x.AuditName).HasConversion(
                n => n.Value, v => Name.Create(v)).HasMaxLength(50);
        });
        builder.OwnsOne(s => s.DeletedBy, a =>
        {
            a.Property(x => x.AuditName).HasConversion(
                n => n.Value, v => Name.Create(v)).HasMaxLength(50);
        });
    }
}
