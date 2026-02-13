using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class EmailAddressTypeConfiguration : IEntityTypeConfiguration<EmailAddressType>
{
    public void Configure(EntityTypeBuilder<EmailAddressType> builder)
    {
        builder.HasKey(e => e.CtrlNbr);

        builder.Property(e => e.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(e => e.ClientCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Number).IsRequired();
        builder.Property(e => e.EmergencyType).IsRequired();

        builder.OwnsOne(e => e.CreatedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(e => e.ModifiedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(e => e.DeletedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });
    }
}