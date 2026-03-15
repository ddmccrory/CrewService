using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class VoluntaryReferralConfiguration : IEntityTypeConfiguration<VoluntaryReferral>
{
    public void Configure(EntityTypeBuilder<VoluntaryReferral> builder)
    {
        builder.HasKey(v => v.CtrlNbr);
        builder.Property(v => v.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(v => v.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(v => v.ReturnToDutyResult).HasMaxLength(20);
        builder.Property(v => v.Status).HasMaxLength(20).IsRequired();

        builder.HasOne<Employee>().WithMany().HasForeignKey(v => v.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(v => v.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(v => v.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(v => v.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
