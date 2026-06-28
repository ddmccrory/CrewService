using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.HasKey(a => a.CtrlNbr);
        builder.Property(a => a.CtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.GroupCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(a => a.GroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.Property(a => a.DepartmentCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));
        builder.HasOne<Department>().WithMany().HasForeignKey(a => a.DepartmentCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.Property(a => a.Code).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.IsExtra).IsRequired();
        builder.Property(a => a.IsActive).IsRequired();
        builder.HasIndex(a => a.GroupCtrlNbr);
        builder.OwnsOne(a => a.CreatedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.ModifiedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.DeletedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class AssignmentScheduleConfiguration : IEntityTypeConfiguration<AssignmentSchedule>
{
    public void Configure(EntityTypeBuilder<AssignmentSchedule> builder)
    {
        builder.HasKey(s => s.CtrlNbr);
        builder.Property(s => s.CtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.AssignmentCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.HasOne<Assignment>().WithMany().HasForeignKey(s => s.AssignmentCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.Property(s => s.ShiftDefinitionCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.HasOne<ShiftDefinition>().WithMany().HasForeignKey(s => s.ShiftDefinitionCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.Property(s => s.OperatingDaysMask).IsRequired();
        builder.HasIndex(s => s.AssignmentCtrlNbr);
        builder.HasIndex(s => s.ShiftDefinitionCtrlNbr);
        builder.OwnsOne(s => s.CreatedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.ModifiedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.DeletedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
