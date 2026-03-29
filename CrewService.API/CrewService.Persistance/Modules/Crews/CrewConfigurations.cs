using CrewService.Domain.Modules.Crews;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Modules.Crews;

internal class CrewConfiguration : IEntityTypeConfiguration<Crew>
{
    public void Configure(EntityTypeBuilder<Crew> builder)
    {
        builder.HasKey(c => c.CtrlNbr);
        builder.Property(c => c.CtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.CrewType).HasMaxLength(20).IsRequired();
        builder.Property(c => c.HomeGroupCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();
        builder.OwnsOne(c => c.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class CrewPositionConfiguration : IEntityTypeConfiguration<CrewPosition>
{
    public void Configure(EntityTypeBuilder<CrewPosition> builder)
    {
        builder.HasKey(p => p.CtrlNbr);
        builder.Property(p => p.CtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.CrewCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(p => p.PositionRoleCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(p => p.DisplayOrder).IsRequired();
        builder.OwnsOne(p => p.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class CrewIncumbencyConfiguration : IEntityTypeConfiguration<CrewIncumbency>
{
    public void Configure(EntityTypeBuilder<CrewIncumbency> builder)
    {
        builder.HasKey(i => i.CtrlNbr);
        builder.Property(i => i.CtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v));
        builder.Property(i => i.CrewPositionCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(i => i.EmployeeCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(i => i.StartUtc).IsRequired();
        builder.OwnsOne(i => i.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(i => i.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(i => i.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class CrewAttachmentTemplateConfiguration : IEntityTypeConfiguration<CrewAttachmentTemplate>
{
    public void Configure(EntityTypeBuilder<CrewAttachmentTemplate> builder)
    {
        builder.HasKey(a => a.CtrlNbr);
        builder.Property(a => a.CtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.AssignmentGroupCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(a => a.CrewCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(a => a.StartUtc).IsRequired();
        builder.OwnsOne(a => a.CreatedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.ModifiedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.DeletedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class CrewAttachmentInstanceConfiguration : IEntityTypeConfiguration<CrewAttachmentInstance>
{
    public void Configure(EntityTypeBuilder<CrewAttachmentInstance> builder)
    {
        builder.HasKey(a => a.CtrlNbr);
        builder.Property(a => a.CtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.WorkInstanceCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(a => a.CrewCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(a => a.StartUtc).IsRequired();
        builder.OwnsOne(a => a.CreatedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.ModifiedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.DeletedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class ReliefCoverageRuleConfiguration : IEntityTypeConfiguration<ReliefCoverageRule>
{
    public void Configure(EntityTypeBuilder<ReliefCoverageRule> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.ReliefCrewCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(r => r.AssignmentGroupCtrlNbr).HasConversion(cn => cn.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(r => r.DaysOfWeekMask).IsRequired();
        builder.Property(r => r.StartUtc).IsRequired();
        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
