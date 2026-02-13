using CrewService.Domain.Models.Parents;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class ParentConfiguration : IEntityTypeConfiguration<Parent>
{
    public void Configure(EntityTypeBuilder<Parent> builder)
    {
        builder.HasKey(c => c.CtrlNbr);

        builder.HasMany(c => c.Railroads)
               .WithOne()
               .HasForeignKey(rr => rr.ParentCtrlNbr)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.CtrlNbr).HasConversion(
            parentCtrlNbr => parentCtrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(c => c.Name).HasConversion(
            parentName => parentName.Value,
            value => Name.Create(value));

        builder.Property(c => c.Name).HasMaxLength(100);

        builder.HasIndex(c => c.Name).IsUnique();

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

        builder.OwnsOne(c => c.DeletedBy, deletedByBuilder =>
        {
            deletedByBuilder.Property(a => a.AuditName).HasConversion(
                auditName => auditName.Value,
                value => Name.Create(value));

            deletedByBuilder.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}
