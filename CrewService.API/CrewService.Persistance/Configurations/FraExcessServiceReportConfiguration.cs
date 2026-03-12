using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class FraExcessServiceReportConfiguration : IEntityTypeConfiguration<FraExcessServiceReport>
{
    public void Configure(EntityTypeBuilder<FraExcessServiceReport> builder)
    {
        builder.HasKey(r => r.CtrlNbr);

        builder.Property(r => r.CtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.DutyTourCtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.EmployeeCtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));

        builder.Property(r => r.ViolationType).HasMaxLength(100).IsRequired();
        builder.Property(r => r.ExplanationText).HasMaxLength(1000);

        builder.OwnsOne(r => r.CreatedBy, a =>
        {
            a.Property(x => x.AuditName).HasConversion(
                n => n.Value, v => Name.Create(v)).HasMaxLength(50);
        });
        builder.OwnsOne(r => r.ModifiedBy, a =>
        {
            a.Property(x => x.AuditName).HasConversion(
                n => n.Value, v => Name.Create(v)).HasMaxLength(50);
        });
        builder.OwnsOne(r => r.DeletedBy, a =>
        {
            a.Property(x => x.AuditName).HasConversion(
                n => n.Value, v => Name.Create(v)).HasMaxLength(50);
        });
    }
}
