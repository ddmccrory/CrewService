using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Modules.Policies;

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
        builder.Property(c => c.CtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.EmployeeCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(c => c.CraftCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(c => c.OpenedUtc).IsRequired();
        builder.Property(c => c.ExpiresUtc).IsRequired();
        builder.Property(c => c.Status).HasMaxLength(30).IsRequired();
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
        builder.Property(c => c.CtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.CaseCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(c => c.TargetEmployeeCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(c => c.SubmittedUtc).IsRequired();
        builder.Property(c => c.Decision).HasMaxLength(30);
        builder.Property(c => c.Reason).HasMaxLength(500);
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
        builder.Property(p => p.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(p => p.EligibilityDays).IsRequired();
        builder.Property(p => p.SeniorityBasis).HasMaxLength(50).IsRequired();
        builder.OwnsOne(p => p.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class SeniorityMoveConfiguration : IEntityTypeConfiguration<SeniorityMove>
{
    public void Configure(EntityTypeBuilder<SeniorityMove> builder)
    {
        builder.HasKey(m => m.CtrlNbr);
        builder.Property(m => m.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(m => m.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(m => m.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(m => m.TargetPositionCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(m => m.DisplacedEmployeeCtrlNbr).HasConversion(c => c == null ? (long?)null : c.Value, v => v.HasValue ? ControlNumber.Create(v.Value) : null);
        builder.Property(m => m.ExercisedUtc).IsRequired();
        builder.Property(m => m.DaysOnCurrentPosition).IsRequired();
        builder.OwnsOne(m => m.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(m => m.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(m => m.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
