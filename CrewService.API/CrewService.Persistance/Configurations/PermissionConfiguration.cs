using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(p => p.CtrlNbr);

        builder.Property(p => p.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(p => p.RoleCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(p => p.FeatureCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(p => p.AccessLevel).IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.ParentCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));

        builder.Property(p => p.CraftCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));

        builder.HasIndex(p => new { p.RoleCtrlNbr, p.FeatureCtrlNbr, p.ParentCtrlNbr, p.CraftCtrlNbr }).IsUnique();

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(p => p.RoleCtrlNbr)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Feature>()
            .WithMany()
            .HasForeignKey(p => p.FeatureCtrlNbr)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Craft>()
            .WithMany()
            .HasForeignKey(p => p.CraftCtrlNbr)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(p => p.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(p => p.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(p => p.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}
