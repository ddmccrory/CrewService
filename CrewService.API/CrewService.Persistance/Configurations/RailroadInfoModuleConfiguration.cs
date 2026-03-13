using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class RailroadInformationConfiguration : IEntityTypeConfiguration<RailroadInformation>
{
    public void Configure(EntityTypeBuilder<RailroadInformation> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.WorkAreaGroupCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.InformationType).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Subject).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Body).IsRequired();
        builder.Property(r => r.Status).HasMaxLength(20).IsRequired();

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
