using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
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

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(c => c.HomeGroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);

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

        builder.HasOne<Crew>().WithMany().HasForeignKey(p => p.CrewCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PositionRole>().WithMany().HasForeignKey(p => p.PositionRoleCtrlNbr).OnDelete(DeleteBehavior.Restrict);

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

        builder.HasOne<CrewPosition>().WithMany().HasForeignKey(i => i.CrewPositionCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Employee>().WithMany().HasForeignKey(i => i.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(i => i.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(i => i.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(i => i.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class CrewAssignmentConfiguration : IEntityTypeConfiguration<CrewAssignment>
{
    public void Configure(EntityTypeBuilder<CrewAssignment> builder)
    {
        builder.HasKey(a => a.CtrlNbr);
        builder.Property(a => a.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.CrewCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.AssignmentGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.DaysOfWeekMask).IsRequired();
        builder.Property(a => a.StartUtc).IsRequired();

        builder.HasOne<Crew>().WithMany().HasForeignKey(a => a.CrewCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(a => a.AssignmentGroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(a => a.CreatedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.ModifiedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.DeletedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
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

        builder.HasOne<WorkInstance>().WithMany().HasForeignKey(i => i.WorkInstanceCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Crew>().WithMany().HasForeignKey(i => i.CrewCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(i => i.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(i => i.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(i => i.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}