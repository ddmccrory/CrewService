using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Modules.Boards;

internal class ExtraBoardConfiguration : IEntityTypeConfiguration<ExtraBoard>
{
    public void Configure(EntityTypeBuilder<ExtraBoard> builder)
    {
        builder.HasKey(b => b.CtrlNbr);
        builder.Property(b => b.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(b => b.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(b => b.PlacedGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(b => b.BoardKind).HasMaxLength(20).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.IsActive).IsRequired();
        builder.Property(b => b.AuxBoardType).HasMaxLength(50);
        builder.OwnsOne(b => b.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(b => b.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(b => b.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class BoardMemberConfiguration : IEntityTypeConfiguration<BoardMember>
{
    public void Configure(EntityTypeBuilder<BoardMember> builder)
    {
        builder.HasKey(m => m.CtrlNbr);
        builder.Property(m => m.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(m => m.ExtraBoardCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(m => m.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(m => m.OrderIndex).IsRequired();
        builder.Property(m => m.StateJson).HasMaxLength(4000);
        builder.Property(m => m.StartUtc).IsRequired();
        builder.OwnsOne(m => m.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(m => m.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(m => m.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class BoardCascadePolicyConfiguration : IEntityTypeConfiguration<BoardCascadePolicy>
{
    public void Configure(EntityTypeBuilder<BoardCascadePolicy> builder)
    {
        builder.HasKey(p => p.CtrlNbr);
        builder.Property(p => p.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(p => p.WorkAreaGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(p => p.CraftCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v)).IsRequired();
        builder.Property(p => p.CascadeMode).HasMaxLength(30).IsRequired();
        builder.Property(p => p.AuxEnabled).IsRequired();
        builder.Property(p => p.SelectionStrategy).HasMaxLength(50);
        builder.OwnsOne(p => p.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(p => p.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
