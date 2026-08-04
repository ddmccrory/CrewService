using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal sealed class WorkflowTemplateConfiguration : IEntityTypeConfiguration<WorkflowTemplate>
{
    public void Configure(EntityTypeBuilder<WorkflowTemplate> builder)
    {
        builder.HasKey(w => w.CtrlNbr);

        builder.Property(w => w.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(w => w.RailroadCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.TriggerTypeCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr == null ? (long?)null : ctrlNbr.Value,
            value => value.HasValue ? ControlNumber.Create(value.Value) : null);
        builder.Property(w => w.IsEnabled).IsRequired();

        builder.HasOne<DynamicGroup>()
            .WithMany()
            .HasForeignKey(w => w.RailroadCtrlNbr)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WorkflowTriggerType>()
            .WithMany()
            .HasForeignKey(w => w.TriggerTypeCtrlNbr)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => new { w.RailroadCtrlNbr, w.TriggerTypeCtrlNbr });

        builder.OwnsOne(w => w.CreatedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
        builder.OwnsOne(w => w.ModifiedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
        builder.OwnsOne(w => w.DeletedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
    }
}

internal sealed class WorkflowTriggerTypeConfiguration : IEntityTypeConfiguration<WorkflowTriggerType>
{
    public void Configure(EntityTypeBuilder<WorkflowTriggerType> builder)
    {
        builder.HasKey(w => w.CtrlNbr);
        builder.Property(w => w.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));
        builder.Property(w => w.Code).HasMaxLength(100).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.IsActive).IsRequired();
        builder.HasIndex(w => w.Code).IsUnique();

        builder.OwnsOne(w => w.CreatedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
        builder.OwnsOne(w => w.ModifiedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
        builder.OwnsOne(w => w.DeletedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
    }
}

internal sealed class WorkflowEffectTypeConfiguration : IEntityTypeConfiguration<WorkflowEffectType>
{
    public void Configure(EntityTypeBuilder<WorkflowEffectType> builder)
    {
        builder.HasKey(w => w.CtrlNbr);
        builder.Property(w => w.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));
        builder.Property(w => w.Code).HasMaxLength(100).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.IsActive).IsRequired();
        builder.HasIndex(w => w.Code).IsUnique();

        builder.OwnsOne(w => w.CreatedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
        builder.OwnsOne(w => w.ModifiedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
        builder.OwnsOne(w => w.DeletedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
    }
}

internal sealed class WorkflowOperatorTypeConfiguration : IEntityTypeConfiguration<WorkflowOperatorType>
{
    public void Configure(EntityTypeBuilder<WorkflowOperatorType> builder)
    {
        builder.HasKey(w => w.CtrlNbr);
        builder.Property(w => w.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));
        builder.Property(w => w.Code).HasMaxLength(100).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.IsActive).IsRequired();
        builder.HasIndex(w => w.Code).IsUnique();

        builder.OwnsOne(w => w.CreatedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
        builder.OwnsOne(w => w.ModifiedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
        builder.OwnsOne(w => w.DeletedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
    }
}

internal sealed class WorkflowMetadataFieldTypeConfiguration : IEntityTypeConfiguration<WorkflowMetadataFieldType>
{
    public void Configure(EntityTypeBuilder<WorkflowMetadataFieldType> builder)
    {
        builder.HasKey(w => w.CtrlNbr);
        builder.Property(w => w.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));
        builder.Property(w => w.Code).HasMaxLength(100).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.IsActive).IsRequired();
        builder.HasIndex(w => w.Code).IsUnique();

        builder.OwnsOne(w => w.CreatedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
        builder.OwnsOne(w => w.ModifiedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
        builder.OwnsOne(w => w.DeletedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
    }
}

internal sealed class WorkflowVersionConfiguration : IEntityTypeConfiguration<WorkflowVersion>
{
    public void Configure(EntityTypeBuilder<WorkflowVersion> builder)
    {
        builder.HasKey(w => w.CtrlNbr);

        builder.Property(w => w.CtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(w => w.WorkflowTemplateCtrlNbr).HasConversion(
            ctrlNbr => ctrlNbr.Value,
            value => ControlNumber.Create(value));

        builder.Property(w => w.VersionNumber).IsRequired();
        builder.Property(w => w.Status).HasMaxLength(30).IsRequired();
        builder.Property(w => w.DefinitionJson).IsRequired();
        builder.Property(w => w.Notes).HasMaxLength(2000).IsRequired();
        builder.Property(w => w.SavedAtUtc).IsRequired();

        builder.HasOne<WorkflowTemplate>()
            .WithMany()
            .HasForeignKey(w => w.WorkflowTemplateCtrlNbr)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => new { w.WorkflowTemplateCtrlNbr, w.VersionNumber }).IsUnique();
        builder.HasIndex(w => new { w.Status, w.PublishedAtUtc });

        builder.OwnsOne(w => w.CreatedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
        builder.OwnsOne(w => w.ModifiedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
        builder.OwnsOne(w => w.DeletedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
    }
}
