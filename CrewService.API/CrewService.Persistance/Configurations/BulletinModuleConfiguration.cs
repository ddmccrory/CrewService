using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class PositionVacancyConfiguration : IEntityTypeConfiguration<PositionVacancy>
{
    public void Configure(EntityTypeBuilder<PositionVacancy> builder)
    {
        builder.HasKey(v => v.CtrlNbr);
        builder.Property(v => v.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(v => v.TargetCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(v => v.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(v => v.PreviousIncumbentCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(v => v.TargetType).HasMaxLength(30).IsRequired();
        builder.Property(v => v.TargetName).HasMaxLength(200).IsRequired();
        builder.Property(v => v.VacancyReasonCode).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Status).HasMaxLength(30).IsRequired();

        builder.Property(v => v.WorkAreaGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(v => v.WorkAreaGroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Craft>().WithMany().HasForeignKey(v => v.CraftCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(v => v.PreviousIncumbentCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(v => v.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(v => v.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(v => v.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class BulletinConfiguration : IEntityTypeConfiguration<Bulletin>
{
    public void Configure(EntityTypeBuilder<Bulletin> builder)
    {
        builder.HasKey(b => b.CtrlNbr);
        builder.Property(b => b.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.PositionVacancyCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.AwardedEmployeeCtrlNbr).HasConversion(
            c => c == null ? (long?)null : c.Value,
            v => v == null ? null : ControlNumber.Create(v.Value));
        builder.Property(b => b.Status).HasMaxLength(30).IsRequired();
        builder.Property(b => b.AwardType).HasMaxLength(20);

        builder.HasOne<PositionVacancy>().WithMany().HasForeignKey(b => b.PositionVacancyCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Craft>().WithMany().HasForeignKey(b => b.CraftCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(b => b.AwardedEmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(b => b.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(b => b.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(b => b.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class BulletinBidConfiguration : IEntityTypeConfiguration<BulletinBid>
{
    public void Configure(EntityTypeBuilder<BulletinBid> builder)
    {
        builder.HasKey(b => b.CtrlNbr);
        builder.Property(b => b.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.BulletinCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.Status).HasMaxLength(20).IsRequired();

        builder.HasOne<Bulletin>().WithMany().HasForeignKey(b => b.BulletinCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Employee>().WithMany().HasForeignKey(b => b.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(b => b.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(b => b.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(b => b.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }

}
internal class BulletinRuleConfiguration : IEntityTypeConfiguration<BulletinRule>
{
	public void Configure(EntityTypeBuilder<BulletinRule> builder)
	{
		builder.HasKey(r => r.CtrlNbr);
		builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
		builder.Property(r => r.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));

		builder.HasIndex(r => r.CraftCtrlNbr).IsUnique();
		builder.HasOne<Craft>().WithMany().HasForeignKey(r => r.CraftCtrlNbr).OnDelete(DeleteBehavior.Cascade);

		builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
		builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
		builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
	}
}
