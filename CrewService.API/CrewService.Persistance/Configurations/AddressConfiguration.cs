using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.HasKey(a => a.CtrlNbr);

        builder.Property(a => a.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(a => a.EmployeeCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(a => a.AddressTypeCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(a => a.Address1).HasMaxLength(250).IsRequired();
        builder.Property(a => a.Address2).HasMaxLength(250);
        builder.Property(a => a.City).HasMaxLength(250).IsRequired();
        builder.Property(a => a.State).HasMaxLength(2).IsRequired();
        builder.Property(a => a.ZipCode).HasMaxLength(10).IsRequired();

        builder.OwnsOne(a => a.CreatedBy, audit =>
        {
            audit.Property(ab => ab.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(ab => ab.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(a => a.ModifiedBy, audit =>
        {
            audit.Property(ab => ab.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(ab => ab.AuditName).HasMaxLength(50);
        });
    }
}