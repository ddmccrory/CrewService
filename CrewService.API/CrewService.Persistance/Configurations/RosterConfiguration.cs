using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class RosterConfiguration : IEntityTypeConfiguration<Roster>
{
    public void Configure(EntityTypeBuilder<Roster> builder)
    {
        builder.HasKey(r => r.CtrlNbr);

        builder.Property(r => r.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(r => r.CraftCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(r => r.WorkAreaGroupCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(r => r.RailroadPayrollDepartmentCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));

        builder.Property(r => r.RosterName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.RosterPluralName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.RosterNumber).IsRequired();
        builder.Property(r => r.RosterType).IsRequired().HasDefaultValue(RosterType.Active);

        builder.HasOne<Craft>().WithMany().HasForeignKey(r => r.CraftCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(r => r.WorkAreaGroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(r => r.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(r => r.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(r => r.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}