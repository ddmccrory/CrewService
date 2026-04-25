using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class QualificationTypeConfiguration : IEntityTypeConfiguration<QualificationType>
{
    public void Configure(EntityTypeBuilder<QualificationType> builder)
    {
        builder.HasKey(qt => qt.CtrlNbr);

        builder.Property(qt => qt.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(qt => qt.ParentCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(qt => qt.ScopeGroupCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));

        builder.Property(qt => qt.CraftCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));

        builder.Property(qt => qt.RegulatoryQualificationCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));

        builder.Property(qt => qt.Code).HasMaxLength(50).IsRequired();
        builder.Property(qt => qt.Name).HasMaxLength(200).IsRequired();
        builder.Property(qt => qt.Description).HasMaxLength(500);
        builder.Property(qt => qt.EvaluationStrategy).HasMaxLength(20).IsRequired();
        builder.Property(qt => qt.GraceDays).IsRequired();
        builder.Property(qt => qt.RenewalLeadDays).IsRequired();
        builder.Property(qt => qt.CalendarYearExpiry).IsRequired();
        builder.Property(qt => qt.IsBlocking).IsRequired();
        builder.Property(qt => qt.IsSystemSeeded).IsRequired();
        builder.Property(qt => qt.IsActive).IsRequired();
        builder.Property(qt => qt.RestrictionLabel).HasMaxLength(50);

        builder.HasIndex(qt => new { qt.ParentCtrlNbr, qt.Code }).IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne<DynamicGroup>().WithMany().HasForeignKey(qt => qt.ScopeGroupCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Craft>().WithMany().HasForeignKey(qt => qt.CraftCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RegulatoryQualification>().WithMany().HasForeignKey(qt => qt.RegulatoryQualificationCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Parent>().WithMany().HasForeignKey(qt => qt.ParentCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(qt => qt.Requirements)
            .WithOne()
            .HasForeignKey(p => p.QualificationTypeCtrlNbr)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(qt => qt.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(qt => qt.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(qt => qt.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}

internal class QualificationRequirementConfiguration : IEntityTypeConfiguration<QualificationRequirement>
{
    public void Configure(EntityTypeBuilder<QualificationRequirement> builder)
    {
        builder.HasKey(p => p.CtrlNbr);

        builder.Property(p => p.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(p => p.QualificationTypeCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(p => p.RequiredQualTypeCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));

        builder.Property(p => p.RequiredRegulatoryQualCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value == null ? null : ControlNumber.Create(value.Value));

        builder.Property(p => p.RequirementKind).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Threshold).IsRequired();
        builder.Property(p => p.ThresholdUnit).HasMaxLength(10).IsRequired();
        builder.Property(p => p.EventSource).HasMaxLength(30);
        builder.Property(p => p.ActivityFilter).HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(200).IsRequired();

        builder.HasOne<QualificationType>()
            .WithMany()
            .HasForeignKey(p => p.RequiredQualTypeCtrlNbr)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RegulatoryQualification>()
            .WithMany()
            .HasForeignKey(p => p.RequiredRegulatoryQualCtrlNbr)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(p => p.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(p => p.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });

        builder.OwnsOne(p => p.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
            audit.Property(a => a.AuditName).HasMaxLength(50);
        });
    }
}
