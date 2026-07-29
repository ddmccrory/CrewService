using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class BoardSnapshotConfiguration : IEntityTypeConfiguration<BoardSnapshot>
{
    public void Configure(EntityTypeBuilder<BoardSnapshot> builder)
    {
        builder.HasKey(s => s.CtrlNbr);
        builder.Property(s => s.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.ShiftInstanceCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.PositionSlotInstanceCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(s => s.VacancyImpactCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(s => s.CapturedAtUtc).IsRequired();
        builder.Property(s => s.TriggerSource).HasMaxLength(50).IsRequired();
        builder.Property(s => s.DecisionSequence).IsRequired();

        builder.HasMany(s => s.Rows).WithOne().HasForeignKey(r => r.BoardSnapshotCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ShiftInstance>().WithMany().HasForeignKey(s => s.ShiftInstanceCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PositionSlotInstance>().WithMany().HasForeignKey(s => s.PositionSlotInstanceCtrlNbr).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VacancyImpact>().WithMany().HasForeignKey(s => s.VacancyImpactCtrlNbr).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.ShiftInstanceCtrlNbr, s.DecisionSequence }).IsUnique();

        builder.OwnsOne(s => s.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class BoardSnapshotRowConfiguration : IEntityTypeConfiguration<BoardSnapshotRow>
{
    public void Configure(EntityTypeBuilder<BoardSnapshotRow> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.BoardSnapshotCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.BoardSlotInstanceCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.ShiftInstanceCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.RosterBoardCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.RosterBoardPositionCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(r => r.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.Status).HasMaxLength(20).IsRequired();
        builder.Property(r => r.BoardName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.EmployeeName).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(r => r.PositionName).HasMaxLength(100).HasDefaultValue(string.Empty);
        builder.Property(r => r.TieUpAtUtc);

        builder.HasOne<BoardSlotInstance>().WithMany().HasForeignKey(r => r.BoardSlotInstanceCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ShiftInstance>().WithMany().HasForeignKey(r => r.ShiftInstanceCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RosterBoard>().WithMany().HasForeignKey(r => r.RosterBoardCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RosterBoardPosition>().WithMany().HasForeignKey(r => r.RosterBoardPositionCtrlNbr).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.BoardSnapshotCtrlNbr, r.BoardOrder, r.CallSequence, r.CtrlNbr }).IsUnique();

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class BoardSelectionDecisionConfiguration : IEntityTypeConfiguration<BoardSelectionDecision>
{
    public void Configure(EntityTypeBuilder<BoardSelectionDecision> builder)
    {
        builder.HasKey(d => d.CtrlNbr);
        builder.Property(d => d.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(d => d.ShiftInstanceCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(d => d.PositionSlotInstanceCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(d => d.VacancyImpactCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(d => d.SnapshotCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(d => d.SelectedBoardSlotInstanceCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(d => d.SelectedEmployeeCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(d => d.OccurredAtUtc).IsRequired();
        builder.Property(d => d.DecisionSource).HasMaxLength(50).IsRequired();
        builder.Property(d => d.DecisionPhase).HasMaxLength(50).IsRequired();
        builder.Property(d => d.DecisionJson).HasMaxLength(8000);

        builder.HasOne<ShiftInstance>().WithMany().HasForeignKey(d => d.ShiftInstanceCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PositionSlotInstance>().WithMany().HasForeignKey(d => d.PositionSlotInstanceCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VacancyImpact>().WithMany().HasForeignKey(d => d.VacancyImpactCtrlNbr).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BoardSnapshot>().WithMany().HasForeignKey(d => d.SnapshotCtrlNbr).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BoardSlotInstance>().WithMany().HasForeignKey(d => d.SelectedBoardSlotInstanceCtrlNbr).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(d => d.SelectedEmployeeCtrlNbr).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.ShiftInstanceCtrlNbr, d.DecisionSequence }).IsUnique();

        builder.OwnsOne(d => d.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(d => d.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(d => d.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
