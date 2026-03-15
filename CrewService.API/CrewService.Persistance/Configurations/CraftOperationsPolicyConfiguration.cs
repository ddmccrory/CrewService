using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class CraftOperationsPolicyConfiguration : IEntityTypeConfiguration<CraftOperationsPolicy>
{
    public void Configure(EntityTypeBuilder<CraftOperationsPolicy> builder)
    {
        builder.HasKey(p => p.CtrlNbr);
        builder.Property(p => p.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.RestCalculationStrategy).HasMaxLength(20).IsRequired();
        builder.Property(p => p.FixedRestHours).HasPrecision(5, 2);
        builder.Property(p => p.ConsecutiveDayResetHours).HasPrecision(5, 2);

        builder.HasIndex(p => p.CraftCtrlNbr).IsUnique();

        builder.HasOne<Craft>().WithMany().HasForeignKey(p => p.CraftCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(p => p.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
