using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class RailroadPoolPayrollTierConfiguration : IEntityTypeConfiguration<RailroadPoolPayrollTier>
{
    public void Configure(EntityTypeBuilder<RailroadPoolPayrollTier> builder)
    {
        builder.HasKey(t => t.CtrlNbr);

        builder.Property(t => t.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(t => t.RailroadPoolCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(t => t.NumberOfDays).IsRequired();
        builder.Property(t => t.TypeOfDay).IsRequired();
        builder.Property(t => t.RatePercentage).IsRequired();

        builder.OwnsOne(t => t.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(t => t.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(t => t.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}