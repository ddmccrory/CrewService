using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class RailroadConfiguration : IEntityTypeConfiguration<Railroad>
{
    public void Configure(EntityTypeBuilder<Railroad> builder)
    {
        builder.HasKey(rr => rr.CtrlNbr);

        builder.Property(rr => rr.CtrlNbr).HasConversion(
            railroadCtrlNbr => railroadCtrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(rr => rr.ParentCtrlNbr).HasConversion(
            parentCtrlNbr => parentCtrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(rr => rr.Name).HasConversion(
            railroadName => railroadName.Value,
            value => Name.Create(value));

        builder.Property(rr => rr.Name).HasMaxLength(100);

        builder.HasIndex(rr => rr.Name).IsUnique();

        builder.OwnsOne(c => c.CreatedBy, createdByBuilder =>
        {
            createdByBuilder.Property(a => a.AuditName).HasConversion(
                auditName => auditName.Value,
                value => Name.Create(value));

            createdByBuilder.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(c => c.ModifiedBy, modifiedByBuilder =>
        {
            modifiedByBuilder.Property(a => a.AuditName).HasConversion(
                auditName => auditName.Value,
                value => Name.Create(value));

            modifiedByBuilder.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}
