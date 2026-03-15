using CrewService.Domain.Modules.HolidayManagement;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class RailroadHolidaySelectionConfiguration : IEntityTypeConfiguration<RailroadHolidaySelection>
{
    public void Configure(EntityTypeBuilder<RailroadHolidaySelection> builder)
    {
        builder.HasKey(s => s.CtrlNbr);
        builder.Property(s => s.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.WorkAreaGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(s => s.HolidayCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(s => new { s.WorkAreaGroupCtrlNbr, s.HolidayCode }).IsUnique();

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(s => s.WorkAreaGroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(s => s.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(s => s.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
