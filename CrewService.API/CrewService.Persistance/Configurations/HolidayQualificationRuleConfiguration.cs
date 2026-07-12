using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class HolidayQualificationRuleConfiguration : IEntityTypeConfiguration<HolidayQualificationRule>
{
    public void Configure(EntityTypeBuilder<HolidayQualificationRule> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.HolidayCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.CraftCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(r => r.ExemptAbsenceCodes).HasMaxLength(500);

        builder.HasOne<Holiday>().WithMany().HasForeignKey(r => r.HolidayCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Craft>().WithMany().HasForeignKey(r => r.CraftCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
