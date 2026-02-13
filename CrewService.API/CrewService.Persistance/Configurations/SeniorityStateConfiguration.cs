using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class SeniorityStateConfiguration : IEntityTypeConfiguration<SeniorityState>
{
    public void Configure(EntityTypeBuilder<SeniorityState> builder)
    {
        builder.HasKey(s => s.CtrlNbr);

        builder.Property(s => s.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(s => s.StateDescription).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Active).IsRequired();
        builder.Property(s => s.CutBack).IsRequired();
        builder.Property(s => s.Inactive).IsRequired();

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