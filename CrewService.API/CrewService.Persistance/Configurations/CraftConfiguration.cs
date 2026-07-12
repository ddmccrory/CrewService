using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class CraftConfiguration : IEntityTypeConfiguration<Craft>
{
    public void Configure(EntityTypeBuilder<Craft> builder)
    {
        builder.HasKey(c => c.CtrlNbr);

        builder.Property(c => c.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(c => c.ParentCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));
        builder.Property(c => c.DynamicGroupCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));

        builder.Property(c => c.CraftName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.CraftPluralName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.CraftNumber).IsRequired();
        builder.Property(c => c.AutoMarkUp).IsRequired();
        builder.Property(c => c.ApproveAllMarkOffs).IsRequired();
        builder.Property(c => c.MarkOffHours).IsRequired();
        builder.Property(c => c.MarkUpHours).IsRequired();
        builder.Property(c => c.RequiredRestHours).IsRequired();
        builder.Property(c => c.MaximumVacationDayTime).IsRequired();
        builder.Property(c => c.UnpaidMealPeriodMinutes).IsRequired();
        builder.Property(c => c.HoursofService).IsRequired();
        builder.Property(c => c.RegulatoryStandardCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));
        builder.Property(c => c.ProcessPayroll).IsRequired();
        builder.Property(c => c.ShowNotifications).IsRequired();
        builder.Property(c => c.VacationAssignmentType).IsRequired();

        builder.Property(c => c.DepartmentCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(c => c.DynamicGroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RegulatoryStandard>().WithMany().HasForeignKey(c => c.RegulatoryStandardCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(c => c.DepartmentCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(c => c.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(c => c.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(c => c.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}