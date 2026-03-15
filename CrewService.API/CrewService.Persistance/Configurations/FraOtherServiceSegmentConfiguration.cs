using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class FraOtherServiceSegmentConfiguration : IEntityTypeConfiguration<FraOtherServiceSegment>
{
    public void Configure(EntityTypeBuilder<FraOtherServiceSegment> builder)
    {
        builder.HasKey(s => s.CtrlNbr);

        builder.Property(s => s.CtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.DutyTourCtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));

        builder.Property(s => s.ServiceTypeCode).HasMaxLength(50).IsRequired();
        builder.Property(s => s.StartLocationCode).HasMaxLength(20).IsRequired();
        builder.Property(s => s.EndLocationCode).HasMaxLength(20).IsRequired();

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
