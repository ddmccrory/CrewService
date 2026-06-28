using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class StaffablePositionConfiguration : IEntityTypeConfiguration<StaffablePosition>
{
    public void Configure(EntityTypeBuilder<StaffablePosition> builder)
    {
        builder.HasKey(s => s.CtrlNbr);
        builder.Property(s => s.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.PositionType).HasMaxLength(20).IsRequired();
        builder.OwnsOne(s => s.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class PositionAssignmentConfiguration : IEntityTypeConfiguration<PositionAssignment>
{
    public void Configure(EntityTypeBuilder<PositionAssignment> builder)
    {
        builder.HasKey(a => a.CtrlNbr);
        builder.Property(a => a.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.StaffablePositionCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(a => a.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(a => a.AssignmentType).HasMaxLength(30).IsRequired();
        builder.Property(a => a.AssignmentSourceCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));
        builder.Property(a => a.AssignedDateUtc).IsRequired();
        builder.HasOne<StaffablePosition>().WithMany().HasForeignKey(a => a.StaffablePositionCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(a => a.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.OwnsOne(a => a.CreatedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.ModifiedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.DeletedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}