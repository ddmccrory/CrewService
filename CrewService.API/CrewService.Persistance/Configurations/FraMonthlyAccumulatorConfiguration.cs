using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class FraMonthlyAccumulatorConfiguration : IEntityTypeConfiguration<FraMonthlyAccumulator>
{
    public void Configure(EntityTypeBuilder<FraMonthlyAccumulator> builder)
    {
        builder.HasKey(a => a.CtrlNbr);

        builder.Property(a => a.CtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));
        builder.Property(a => a.EmployeeCtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));

        builder.Property(a => a.YearMonth).HasMaxLength(7).IsRequired();

        builder.HasIndex(a => new { a.EmployeeCtrlNbr, a.YearMonth }).IsUnique();

        builder.HasOne<Employee>().WithMany().HasForeignKey(a => a.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(a => a.CreatedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.ModifiedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(a => a.DeletedBy, au => { au.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
