using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class DispatchProjectionConfiguration : IEntityTypeConfiguration<DispatchProjection>
{
    public void Configure(EntityTypeBuilder<DispatchProjection> builder)
    {
        builder.HasKey(p => p.CtrlNbr);
        builder.Property(p => p.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.PositionSlotCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.ProjectedEmployeeCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));

        builder.HasOne<PositionSlotInstance>().WithMany().HasForeignKey(p => p.PositionSlotCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(p => p.ProjectedEmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(p => p.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class DispatchDecisionLogConfiguration : IEntityTypeConfiguration<DispatchDecisionLog>
{
    public void Configure(EntityTypeBuilder<DispatchDecisionLog> builder)
    {
        builder.HasKey(l => l.CtrlNbr);
        builder.Property(l => l.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(l => l.PositionSlotCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(l => l.SelectedEmployeeCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(l => l.Phase).HasMaxLength(20).IsRequired();
        builder.Property(l => l.SelectionSource).HasMaxLength(50);

        builder.HasOne<PositionSlotInstance>().WithMany().HasForeignKey(l => l.PositionSlotCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(l => l.SelectedEmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(l => l.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(l => l.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(l => l.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class DispatchOverrideConfiguration : IEntityTypeConfiguration<DispatchOverride>
{
    public void Configure(EntityTypeBuilder<DispatchOverride> builder)
    {
        builder.HasKey(o => o.CtrlNbr);
        builder.Property(o => o.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(o => o.PositionSlotCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(o => o.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(o => o.ApprovedByCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(o => o.OverrideType).HasMaxLength(50).IsRequired();
        builder.Property(o => o.ReasonCode).HasMaxLength(64).IsRequired();
        builder.Property(o => o.ReasonText).HasMaxLength(512);
        builder.Property(o => o.Status).HasMaxLength(20).IsRequired();

        builder.HasOne<PositionSlotInstance>().WithMany().HasForeignKey(o => o.PositionSlotCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(o => o.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(o => o.ApprovedByCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(o => o.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(o => o.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(o => o.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class EmployeeBookingConfiguration : IEntityTypeConfiguration<EmployeeBooking>
{
    public void Configure(EntityTypeBuilder<EmployeeBooking> builder)
    {
        builder.HasKey(b => b.CtrlNbr);
        builder.Property(b => b.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.PositionSlotCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));

        builder.HasOne<Employee>().WithMany().HasForeignKey(b => b.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PositionSlotInstance>().WithMany().HasForeignKey(b => b.PositionSlotCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(b => b.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(b => b.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(b => b.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
