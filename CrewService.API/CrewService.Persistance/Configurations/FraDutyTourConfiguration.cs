using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class FraDutyTourConfiguration : IEntityTypeConfiguration<FraDutyTour>
{
    public void Configure(EntityTypeBuilder<FraDutyTour> builder)
    {
        builder.HasKey(t => t.CtrlNbr);

        builder.Property(t => t.CtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));

        builder.Property(t => t.EmployeeCtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));

        builder.Property(t => t.RegulatoryStandardCtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));

        builder.Property(t => t.ExcessServiceReason).HasMaxLength(500);

        builder.HasMany(t => t.Segments)
            .WithOne()
            .HasForeignKey(s => s.DutyTourCtrlNbr);

        builder.HasMany(t => t.TransportationSegments)
            .WithOne()
            .HasForeignKey(s => s.DutyTourCtrlNbr);

        builder.HasMany(t => t.OtherServiceSegments)
            .WithOne()
            .HasForeignKey(s => s.DutyTourCtrlNbr);

        builder.OwnsOne(t => t.CreatedBy, a =>
        {
            a.Property(x => x.AuditName).HasConversion(
                n => n.Value, v => Name.Create(v)).HasMaxLength(50);
        });
        builder.OwnsOne(t => t.ModifiedBy, a =>
        {
            a.Property(x => x.AuditName).HasConversion(
                n => n.Value, v => Name.Create(v)).HasMaxLength(50);
        });
        builder.OwnsOne(t => t.DeletedBy, a =>
        {
            a.Property(x => x.AuditName).HasConversion(
                n => n.Value, v => Name.Create(v)).HasMaxLength(50);
        });
    }
}
