using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class RegulatoryQualificationConfiguration : IEntityTypeConfiguration<RegulatoryQualification>
{
    public void Configure(EntityTypeBuilder<RegulatoryQualification> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.Code).HasMaxLength(50).IsRequired();
        builder.Property(r => r.CfrPart).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(200).IsRequired();
        builder.HasIndex(r => r.Code).IsUnique();

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class CraftRegulatoryQualificationConfiguration : IEntityTypeConfiguration<CraftRegulatoryQualification>
{
    public void Configure(EntityTypeBuilder<CraftRegulatoryQualification> builder)
    {
        builder.HasKey(c => c.CtrlNbr);
        builder.Property(c => c.CtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.CraftCtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.Property(c => c.RegulatoryQualificationCtrlNbr).HasConversion(x => x.Value, v => ControlNumber.Create(v));
        builder.HasIndex(c => new { c.CraftCtrlNbr, c.RegulatoryQualificationCtrlNbr }).IsUnique();

        builder.OwnsOne(c => c.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(c => c.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
