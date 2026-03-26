using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Modules.WorkManagement;

internal class AssignmentTemplateConfiguration : IEntityTypeConfiguration<AssignmentTemplate>
{
    public void Configure(EntityTypeBuilder<AssignmentTemplate> builder)
    {
        builder.HasKey(t => t.CtrlNbr);
        builder.Property(t => t.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(t => t.WorkAreaGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(t => t.Code).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.RecurrenceJson).HasMaxLength(4000);
        builder.Property(t => t.IsActive).IsRequired();
        builder.OwnsOne(t => t.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(t => t.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(t => t.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class WorkInstanceConfiguration : IEntityTypeConfiguration<WorkInstance>
{
    public void Configure(EntityTypeBuilder<WorkInstance> builder)
    {
        builder.HasKey(w => w.CtrlNbr);
        builder.Property(w => w.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(w => w.AssignmentTemplateCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value, v => v.HasValue ? ControlNumber.Create(v.Value) : null);
        builder.Property(w => w.WorkAreaGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(w => w.StartUtc).IsRequired();
        builder.Property(w => w.EndUtc).IsRequired();
        builder.Property(w => w.Status).HasMaxLength(30).IsRequired();
        builder.OwnsOne(w => w.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(w => w.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(w => w.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class PositionRoleConfiguration : IEntityTypeConfiguration<PositionRole>
{
    public void Configure(EntityTypeBuilder<PositionRole> builder)
    {
        builder.HasKey(p => p.CtrlNbr);
        builder.Property(p => p.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(50);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.AlternateName).HasMaxLength(100);
        builder.OwnsOne(p => p.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class PositionSlotConfiguration : IEntityTypeConfiguration<PositionSlot>
{
    public void Configure(EntityTypeBuilder<PositionSlot> builder)
    {
        builder.HasKey(s => s.CtrlNbr);
        builder.Property(s => s.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.WorkInstanceCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(s => s.PositionRoleCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(s => s.Status).HasMaxLength(30).IsRequired();
        builder.Property(s => s.BoundEmployeeCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value, v => v.HasValue ? ControlNumber.Create(v.Value) : null);
        builder.Property(s => s.BindingSource).HasMaxLength(50);
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
        builder.Property(r => r.PositionSlotCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(r => r.Priority).IsRequired();
        builder.Property(r => r.PositionRoleCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value, v => v.HasValue ? ControlNumber.Create(v.Value) : null);
        builder.Property(r => r.QualificationTypeCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value, v => v.HasValue ? ControlNumber.Create(v.Value) : null);
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
