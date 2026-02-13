using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class SeniorityConfiguration : IEntityTypeConfiguration<Seniority>
{
    public void Configure(EntityTypeBuilder<Seniority> builder)
    {
        builder.HasKey(s => s.CtrlNbr);

        builder.Property(s => s.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(s => s.RosterCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(s => s.RailroadPoolEmployeeCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(s => s.LastActiveRoster).IsRequired();
        builder.Property(s => s.RosterDate).IsRequired();
        builder.Property(s => s.Rank).IsRequired();
        builder.Property(s => s.StateID).IsRequired();
        builder.Property(s => s.CanTrain).IsRequired();

        builder.OwnsOne(s => s.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(s => s.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(s => s.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}