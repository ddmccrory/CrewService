using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class RegulatoryStandardConfiguration : IEntityTypeConfiguration<RegulatoryStandard>
{
    public void Configure(EntityTypeBuilder<RegulatoryStandard> builder)
    {
        builder.HasKey(r => r.CtrlNbr);

        builder.Property(r => r.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(r => r.Code).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(200).IsRequired();
        builder.Property(r => r.MaxOnDutyMinutes).IsRequired();
        builder.Property(r => r.MinRestMinutes).IsRequired();
        builder.Property(r => r.Min8hRestInPreceding24h).IsRequired();
        builder.Property(r => r.ConsecutiveDayLimit6).IsRequired();
        builder.Property(r => r.ConsecutiveDayLimit7).IsRequired();
        builder.Property(r => r.RestAfter6DaysMinutes).IsRequired();
        builder.Property(r => r.RestAfter7DaysMinutes).IsRequired();
        builder.Property(r => r.MonthlyCapMinutes).IsRequired();
        builder.Property(r => r.DeadheadAfter12hMonthlyCapMinutes).IsRequired();
        builder.Property(r => r.WreckReliefExtraMinutes).IsRequired();

        builder.HasIndex(r => r.Code).IsUnique();

        builder.OwnsOne(r => r.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value, value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(r => r.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value, value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(r => r.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value, value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}
