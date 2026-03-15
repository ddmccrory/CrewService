using CrewService.Domain.Models.Employees;
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
            .HasForeignKey(s => s.DutyTourCtrlNbr)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.TransportationSegments)
            .WithOne()
            .HasForeignKey(s => s.DutyTourCtrlNbr)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.OtherServiceSegments)
            .WithOne()
            .HasForeignKey(s => s.DutyTourCtrlNbr)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Employee>().WithMany().HasForeignKey(t => t.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RegulatoryStandard>().WithMany().HasForeignKey(t => t.RegulatoryStandardCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(t => t.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(t => t.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(t => t.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
