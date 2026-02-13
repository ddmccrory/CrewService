using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class PhoneNumberTypeConfiguration : IEntityTypeConfiguration<PhoneNumberType>
{
    public void Configure(EntityTypeBuilder<PhoneNumberType> builder)
    {
        builder.HasKey(p => p.CtrlNbr);

        builder.Property(p => p.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(p => p.ClientCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Number).IsRequired();
        builder.Property(p => p.EmergencyType).IsRequired();

        builder.OwnsOne(p => p.CreatedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(p => p.ModifiedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(p => p.DeletedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });
    }
}