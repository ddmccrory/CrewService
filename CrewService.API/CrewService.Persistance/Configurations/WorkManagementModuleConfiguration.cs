using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;


internal class WorkInstanceConfiguration : IEntityTypeConfiguration<WorkInstance>
{
    public void Configure(EntityTypeBuilder<WorkInstance> builder)
    {
        builder.HasKey(w => w.CtrlNbr);
        builder.Property(w => w.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(w => w.WorkAreaGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(w => w.AssignmentGroupCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(w => w.Status).HasMaxLength(20).IsRequired();

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(w => w.WorkAreaGroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(w => w.AssignmentGroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(w => w.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(w => w.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(w => w.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class CraftRoleConfiguration : IEntityTypeConfiguration<CraftRole>
{
    public void Configure(EntityTypeBuilder<CraftRole> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.Code).HasMaxLength(30);
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.AlternateName).HasMaxLength(100);

        builder.HasOne<Craft>().WithMany().HasForeignKey(r => r.CraftCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class PositionSlotConfiguration : IEntityTypeConfiguration<PositionSlot>
{
    public void Configure(EntityTypeBuilder<PositionSlot> builder)
    {
        builder.HasKey(s => s.CtrlNbr);
        builder.Property(s => s.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.WorkInstanceCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.CraftRoleCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.BoundEmployeeCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(s => s.Status).HasMaxLength(20).IsRequired();
        builder.Property(s => s.BindingSource).HasMaxLength(50);

        builder.HasOne<WorkInstance>().WithMany().HasForeignKey(s => s.WorkInstanceCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CraftRole>().WithMany().HasForeignKey(s => s.CraftRoleCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(s => s.BoundEmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(s => s.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class SlotRequirementConfiguration : IEntityTypeConfiguration<SlotRequirement>
{
    public void Configure(EntityTypeBuilder<SlotRequirement> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.PositionSlotCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.CraftRoleCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(r => r.QualificationTypeCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(r => r.Notes).HasMaxLength(500);

        builder.HasOne<PositionSlot>().WithMany().HasForeignKey(r => r.PositionSlotCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CraftRole>().WithMany().HasForeignKey(r => r.CraftRoleCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RegulatoryQualification>().WithMany().HasForeignKey(r => r.QualificationTypeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.CtrlNbr);
        builder.Property(d => d.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(d => d.ParentCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(d => d.DynamicGroupCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();
        builder.Property(d => d.DefaultCallSheetView).HasMaxLength(20).IsRequired().HasDefaultValue("Vertical");

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(d => d.DynamicGroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(d => d.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(d => d.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(d => d.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
