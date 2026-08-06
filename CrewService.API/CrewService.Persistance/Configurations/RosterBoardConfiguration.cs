using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class RosterBoardConfiguration : IEntityTypeConfiguration<RosterBoard>
{
    public void Configure(EntityTypeBuilder<RosterBoard> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.RosterCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.BoardType).HasConversion(v => v.ToString(), v => Enum.Parse<BoardType>(v)).HasMaxLength(20).IsRequired();
        builder.Property(r => r.RotationType).HasConversion(v => v.ToString(), v => Enum.Parse<RotationType>(v)).HasMaxLength(30).IsRequired();
        builder.Property(r => r.RequiredPositions).HasDefaultValue(0);
        builder.Property(r => r.AllowBulletinBidding);
        builder.Property(r => r.AllowForceAssign).HasDefaultValue(false);
        builder.Property(r => r.NotifyOnPlacement).HasDefaultValue(false);
        builder.Property(r => r.PlacementRequiresAcknowledgement).HasDefaultValue(false);
        builder.Property(r => r.RequiredPositionsStrategyCtrlNbr)
            .HasConversion(c => c == null ? (long?)null : c.Value, v => v == null ? null : ControlNumber.Create(v.Value));

        builder.HasMany(r => r.Positions).WithOne().HasForeignKey(p => p.RosterBoardCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Craft>().WithMany().HasForeignKey(r => r.CraftCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Roster>().WithMany().HasForeignKey(r => r.RosterCtrlNbr).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RequiredPositionsStrategy>().WithMany()
            .HasForeignKey(r => r.RequiredPositionsStrategyCtrlNbr).OnDelete(DeleteBehavior.SetNull);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class RosterBoardPositionConfiguration : IEntityTypeConfiguration<RosterBoardPosition>
{
    public void Configure(EntityTypeBuilder<RosterBoardPosition> builder)
    {
        builder.HasKey(p => p.CtrlNbr);
        builder.Property(p => p.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.RosterBoardCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.StaffablePositionCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(p => p.TieUpOrderUtc);
        builder.Property(p => p.OrderSeedBoardPosition).IsRequired();

        builder.HasOne<StaffablePosition>().WithOne().HasForeignKey<RosterBoardPosition>(p => p.StaffablePositionCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(p => p.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(p => p.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
