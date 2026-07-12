using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class CraftDisplacementPolicyConfiguration : IEntityTypeConfiguration<CraftDisplacementPolicy>
{
    public void Configure(EntityTypeBuilder<CraftDisplacementPolicy> builder)
    {
        builder.HasKey(p => p.CtrlNbr);
        builder.Property(p => p.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(p => p.WindowHours).IsRequired();
        builder.Property(p => p.SeniorityBasis).HasMaxLength(50).IsRequired();
        builder.Property(p => p.DefaultAction).HasMaxLength(50).IsRequired();
        builder.Property(p => p.EligibilitySelectorJson).HasMaxLength(4000);

        builder.HasOne<Craft>().WithMany().HasForeignKey(p => p.CraftCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(p => p.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class DisplacementCaseConfiguration : IEntityTypeConfiguration<DisplacementCase>
{
    public void Configure(EntityTypeBuilder<DisplacementCase> builder)
    {
        builder.HasKey(c => c.CtrlNbr);
        builder.Property(c => c.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(c => c.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(c => c.OpenedUtc).IsRequired();
        builder.Property(c => c.ExpiresUtc).IsRequired();
        builder.Property(c => c.Status).HasMaxLength(30).IsRequired();

        builder.HasOne<Employee>().WithMany().HasForeignKey(c => c.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Craft>().WithMany().HasForeignKey(c => c.CraftCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(c => c.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class DisplacementClaimConfiguration : IEntityTypeConfiguration<DisplacementClaim>
{
    public void Configure(EntityTypeBuilder<DisplacementClaim> builder)
    {
        builder.HasKey(c => c.CtrlNbr);
        builder.Property(c => c.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.CaseCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(c => c.TargetEmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(c => c.SubmittedUtc).IsRequired();
        builder.Property(c => c.Decision).HasMaxLength(30);
        builder.Property(c => c.Reason).HasMaxLength(500);

        builder.HasOne<DisplacementCase>().WithMany().HasForeignKey(c => c.CaseCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Employee>().WithMany().HasForeignKey(c => c.TargetEmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(c => c.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class BulletinPolicyConfiguration : IEntityTypeConfiguration<BulletinPolicy>
{
    public void Configure(EntityTypeBuilder<BulletinPolicy> builder)
    {
        builder.HasKey(p => p.CtrlNbr);
        builder.Property(p => p.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(p => p.BidWindowHours).IsRequired();
        builder.Property(p => p.ForcedAssignmentEnabled).IsRequired();
        builder.Property(p => p.ForcedAssignmentBasis).HasMaxLength(30).IsRequired();

        builder.HasOne<Craft>().WithMany().HasForeignKey(p => p.CraftCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(p => p.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class SeniorityMovePolicyConfiguration : IEntityTypeConfiguration<SeniorityMovePolicy>
{
    public void Configure(EntityTypeBuilder<SeniorityMovePolicy> builder)
    {
        builder.HasKey(p => p.CtrlNbr);
        builder.Property(p => p.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.RailroadCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(p => p.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.HasIndex(p => new { p.RailroadCtrlNbr, p.CraftCtrlNbr }).IsUnique();
        builder.Property(p => p.RequestHours).IsRequired();
        builder.Property(p => p.CancelHours).IsRequired();
        builder.Property(p => p.AutoApprove).IsRequired();
        builder.Property(p => p.WillWorkEnabled).IsRequired();
        builder.Property(p => p.CrewToCrewStrategy).HasMaxLength(30).IsRequired();
        builder.Property(p => p.CrewToBoardStrategy).HasMaxLength(30).IsRequired();
        builder.Property(p => p.ExtraBoardToCrewStrategy).HasMaxLength(30).IsRequired();
        builder.Property(p => p.HangoutToCrewStrategy).HasMaxLength(30).IsRequired();
        builder.Property(p => p.ExtendedAbsenceToCrewStrategy).HasMaxLength(30).IsRequired();
        builder.Property(p => p.TrainingToCrewStrategy).HasMaxLength(30).IsRequired();
        builder.Property(p => p.NewHireToCrewStrategy).HasMaxLength(30).IsRequired();
        builder.Property(p => p.CrewToCrewEligibilityDays).IsRequired();
        builder.Property(p => p.CrewToBoardEligibilityDays).IsRequired();
        builder.Property(p => p.ExtraBoardToCrewEligibilityDays).IsRequired();
        builder.Property(p => p.HangoutToCrewEligibilityDays).IsRequired();
        builder.Property(p => p.ExtendedAbsenceToCrewEligibilityDays).IsRequired();
        builder.Property(p => p.TrainingToCrewEligibilityDays).IsRequired();
        builder.Property(p => p.NewHireToCrewEligibilityDays).IsRequired();

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(p => p.RailroadCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Craft>().WithMany().HasForeignKey(p => p.CraftCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(p => p.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class CallSheetRuleConfiguration : IEntityTypeConfiguration<CallSheetRule>
{
    public void Configure(EntityTypeBuilder<CallSheetRule> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.DepartmentCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.HasIndex(r => r.DepartmentCtrlNbr).IsUnique();

        builder.Property(r => r.CallLeadMinutes).IsRequired();
        builder.Property(r => r.CallDurationMinutes).IsRequired();
        builder.Property(r => r.HolidayAdjustment).HasMaxLength(30).IsRequired();
        builder.Property(r => r.HolidayCustomOffsetMinutes);
        builder.Property(r => r.GlobalPreCreateOffsetMinutes).IsRequired();
        builder.Property(r => r.IsEnabled).IsRequired();

        builder.HasOne<Department>().WithMany().HasForeignKey(r => r.DepartmentCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class DepartmentReassignmentRuleConfiguration : IEntityTypeConfiguration<DepartmentReassignmentRule>
{
    public void Configure(EntityTypeBuilder<DepartmentReassignmentRule> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.DepartmentCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.HasIndex(r => r.DepartmentCtrlNbr).IsUnique();

        builder.Property(r => r.TargetBoardType)
            .HasConversion(v => v.ToString(), v => Enum.Parse<BoardType>(v))
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(r => r.IsRequired).IsRequired();

        builder.HasOne<Department>().WithMany().HasForeignKey(r => r.DepartmentCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class SeniorityMoveConfiguration : IEntityTypeConfiguration<SeniorityMove>
{
    public void Configure(EntityTypeBuilder<SeniorityMove> builder)
    {
        builder.HasKey(m => m.CtrlNbr);
        builder.Property(m => m.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(m => m.RailroadCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(m => m.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(m => m.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(m => m.TargetPositionCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(m => m.DisplacedEmployeeCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(m => m.RequestedUtc).IsRequired();
        builder.Property(m => m.EffectiveUtc);
        builder.Property(m => m.DaysOnCurrentPosition).IsRequired();
        builder.Property(m => m.MoveType).HasMaxLength(30).IsRequired();
        builder.Property(m => m.Status).HasMaxLength(30).IsRequired();
        builder.Property(m => m.RejectionReason).HasMaxLength(500);
        builder.Property(m => m.CancellationReason).HasMaxLength(500);
        builder.Property(m => m.WillWork);
        builder.HasIndex(m => new { m.EmployeeCtrlNbr, m.Status });
        builder.HasIndex(m => new { m.CraftCtrlNbr, m.Status });

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(m => m.RailroadCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(m => m.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Craft>().WithMany().HasForeignKey(m => m.CraftCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Employee>().WithMany().HasForeignKey(m => m.DisplacedEmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(m => m.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(m => m.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(m => m.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
