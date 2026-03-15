using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class CompensationBalanceConfiguration : IEntityTypeConfiguration<CompensationBalance>
{
    public void Configure(EntityTypeBuilder<CompensationBalance> builder)
    {
        builder.HasKey(b => b.CtrlNbr);
        builder.Property(b => b.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.CompensationType).HasMaxLength(20).IsRequired();
        builder.Property(b => b.BalanceHours).HasPrecision(8, 2);
        builder.HasIndex(b => new { b.EmployeeCtrlNbr, b.CompensationType }).IsUnique();

        builder.HasOne<Employee>().WithMany().HasForeignKey(b => b.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(b => b.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(b => b.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(b => b.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
