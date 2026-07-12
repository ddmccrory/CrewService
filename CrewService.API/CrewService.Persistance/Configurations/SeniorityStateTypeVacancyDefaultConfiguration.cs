using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class SeniorityStateTypeVacancyDefaultConfiguration : IEntityTypeConfiguration<SeniorityStateTypeVacancyDefault>
{
    public void Configure(EntityTypeBuilder<SeniorityStateTypeVacancyDefault> builder)
    {
        builder.HasKey(c => c.CtrlNbr);
        builder.Property(c => c.CtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.ParentCtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.RailroadCtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.StateType)
            .HasConversion(v => v.ToString(), v => Enum.Parse<StateType>(v))
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(c => c.DefaultVacancyAction)
            .HasConversion(v => v.ToString(), v => Enum.Parse<VacancyAction>(v))
            .HasMaxLength(40)
            .IsRequired();

        builder.HasOne<Parent>().WithMany().HasForeignKey(c => c.ParentCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(c => c.RailroadCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.RailroadCtrlNbr, c.StateType }).IsUnique();

        builder.OwnsOne(c => c.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
