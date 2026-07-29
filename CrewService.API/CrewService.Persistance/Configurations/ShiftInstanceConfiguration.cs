using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class ShiftInstanceConfiguration : IEntityTypeConfiguration<ShiftInstance>
{
    public void Configure(EntityTypeBuilder<ShiftInstance> builder)
    {
        builder.HasKey(s => s.CtrlNbr);
        builder.Property(s => s.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.WorkInstanceCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.ShiftDefinitionCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.ShiftCode).HasMaxLength(20).IsRequired();
        builder.Property(s => s.ShiftDisplayName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.DepartmentCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(s => s.DepartmentName).HasMaxLength(100);
        builder.Property(s => s.Status).HasMaxLength(20).IsRequired();

        builder.HasMany(s => s.PositionSlots).WithOne().HasForeignKey(p => p.ShiftInstanceCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.BoardSlots).WithOne().HasForeignKey(b => b.ShiftInstanceCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.AssignmentNotes).WithOne().HasForeignKey(n => n.ShiftInstanceCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<WorkInstance>().WithMany().HasForeignKey(s => s.WorkInstanceCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(s => s.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class PositionSlotInstanceConfiguration : IEntityTypeConfiguration<PositionSlotInstance>
{
    public void Configure(EntityTypeBuilder<PositionSlotInstance> builder)
    {
        builder.HasKey(p => p.CtrlNbr);
        builder.Property(p => p.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.ShiftInstanceCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.CrewPositionCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(p => p.IncumbentEmployeeCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(p => p.Status).HasMaxLength(20).IsRequired()
            .HasConversion(s => s.ToString(), v => Enum.Parse<PositionSlotStatus>(v));
        builder.Property(p => p.AnnulmentReason).HasMaxLength(500);
        builder.Property(p => p.AssignmentCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.AssignmentCode).HasMaxLength(20).IsRequired();
        builder.Property(p => p.AssignmentName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.CraftRoleName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.GroupName).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(p => p.GroupCode).HasMaxLength(50).HasDefaultValue(string.Empty);
        builder.Property(p => p.OnDutyTime);
        builder.Property(p => p.OffDutyTime);
        builder.Property(p => p.CrewName).HasMaxLength(100).HasDefaultValue(string.Empty);
        builder.Property(p => p.CrewType).HasMaxLength(20).HasDefaultValue(string.Empty);

        builder.HasOne<CrewPosition>().WithMany().HasForeignKey(p => p.CrewPositionCtrlNbr).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(p => p.IncumbentEmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(p => p.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class AssignmentNoteConfiguration : IEntityTypeConfiguration<AssignmentNote>
{
    public void Configure(EntityTypeBuilder<AssignmentNote> builder)
    {
        builder.HasKey(n => n.CtrlNbr);
        builder.Property(n => n.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(n => n.ShiftInstanceCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(n => n.AssignmentCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(n => n.NoteText).HasMaxLength(2000).IsRequired();

        builder.HasIndex(n => new { n.ShiftInstanceCtrlNbr, n.AssignmentCtrlNbr }).IsUnique();

        builder.OwnsOne(n => n.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(n => n.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(n => n.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class BoardSlotInstanceConfiguration : IEntityTypeConfiguration<BoardSlotInstance>
{
    public void Configure(EntityTypeBuilder<BoardSlotInstance> builder)
    {
        builder.HasKey(b => b.CtrlNbr);
        builder.Property(b => b.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.ShiftInstanceCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.RosterBoardCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.RosterBoardPositionCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(b => b.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.Status).HasMaxLength(20).IsRequired()
            .HasConversion(s => s.ToString(), v => Enum.Parse<BoardSlotStatus>(v));
        builder.Property(b => b.BoardName).HasMaxLength(100).IsRequired();
        builder.Property(b => b.EmployeeName).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(b => b.PositionName).HasMaxLength(100).HasDefaultValue(string.Empty);
        builder.Property(b => b.TieUpAtUtc);

        builder.HasOne<RosterBoard>().WithMany().HasForeignKey(b => b.RosterBoardCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RosterBoardPosition>().WithMany().HasForeignKey(b => b.RosterBoardPositionCtrlNbr).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(b => b.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(b => b.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(b => b.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(b => b.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}