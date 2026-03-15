using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class UserParentAssignmentConfiguration : IEntityTypeConfiguration<UserParentAssignment>
{
    public void Configure(EntityTypeBuilder<UserParentAssignment> builder)
    {
        builder.HasKey(a => a.CtrlNbr);

        builder.Property(a => a.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(a => a.ParentCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(a => a.UserId).HasMaxLength(128).IsRequired();
        builder.Property(a => a.Role).HasMaxLength(50).IsRequired();

        builder.HasIndex(a => new { a.UserId, a.ParentCtrlNbr }).IsUnique();
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.ParentCtrlNbr);

        builder.HasOne<Parent>().WithMany().HasForeignKey(a => a.ParentCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(a => a.CreatedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(a => a.ModifiedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(a => a.DeletedBy, audit =>
        {
            audit.Property(x => x.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(x => x.AuditName).HasMaxLength(50);
        });
    }
}
