using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.HasKey(h => h.CtrlNbr);
        builder.Property(h => h.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(h => h.WorkAreaGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(h => h.Name).HasMaxLength(100).IsRequired();

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(h => h.WorkAreaGroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(h => h.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(h => h.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(h => h.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
