using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(i => i.CtrlNbr);

        builder.Property(i => i.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(i => i.ParentCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(i => i.Email).HasMaxLength(256).IsRequired();
        builder.Property(i => i.Role).HasMaxLength(50).IsRequired();
        builder.Property(i => i.InvitedByUserId).HasMaxLength(128).IsRequired();
        builder.Property(i => i.Token).HasMaxLength(64).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(i => i.Token).IsUnique();
        builder.HasIndex(i => i.Email);
        builder.HasIndex(i => i.ParentCtrlNbr);
        builder.HasIndex(i => new { i.Email, i.ParentCtrlNbr, i.Status });

        builder.HasOne<Parent>().WithMany().HasForeignKey(i => i.ParentCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.RailroadCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value.HasValue ? ControlNumber.Create(value.Value) : null);

        builder.HasOne<Railroad>().WithMany().HasForeignKey(i => i.RailroadCtrlNbr).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        builder.OwnsOne(i => i.CreatedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(i => i.ModifiedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(i => i.DeletedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });
    }
}
