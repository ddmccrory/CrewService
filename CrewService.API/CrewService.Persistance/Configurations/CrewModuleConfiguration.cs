using CrewService.Domain.Modules.Crews;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class CrewConfiguration : IEntityTypeConfiguration<Crew>
{
    public void Configure(EntityTypeBuilder<Crew> builder)
    {
        builder.HasKey(c => c.CtrlNbr);
        builder.Property(c => c.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.HomeGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.CrewType).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();

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
        builder.Property(p => p.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.CrewCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.PositionRoleCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));

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
        builder.Property(i => i.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(i => i.CrewPositionCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(i => i.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));

        builder.OwnsOne(i => i.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(i => i.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(i => i.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class CrewAttachmentTemplateConfiguration : IEntityTypeConfiguration<CrewAttachmentTemplate>
{
    public void Configure(EntityTypeBuilder<CrewAttachmentTemplate> builder)
    {
        builder.HasKey(t => t.CtrlNbr);
        builder.Property(t => t.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(t => t.AssignmentTemplateCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(t => t.CrewCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));

        builder.OwnsOne(t => t.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(t => t.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(t => t.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class CrewAttachmentInstanceConfiguration : IEntityTypeConfiguration<CrewAttachmentInstance>
{
    public void Configure(EntityTypeBuilder<CrewAttachmentInstance> builder)
    {
        builder.HasKey(i => i.CtrlNbr);
        builder.Property(i => i.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(i => i.WorkInstanceCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(i => i.CrewCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));

        builder.OwnsOne(i => i.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(i => i.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(i => i.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class ReliefCoverageRuleConfiguration : IEntityTypeConfiguration<ReliefCoverageRule>
{
    public void Configure(EntityTypeBuilder<ReliefCoverageRule> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.ReliefCrewCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.AssignmentTemplateCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
