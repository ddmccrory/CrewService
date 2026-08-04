using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal sealed class WorkflowExecutionHistoryConfiguration : IEntityTypeConfiguration<WorkflowExecutionHistory>
{
    public void Configure(EntityTypeBuilder<WorkflowExecutionHistory> builder)
    {
        builder.HasKey(x => x.CtrlNbr);

        builder.Property(x => x.CtrlNbr)
            .HasConversion(x => x.Value, v => ControlNumber.Create(v));

        builder.Property(x => x.WorkflowTemplateCtrlNbr)
            .HasConversion(x => x.Value, v => ControlNumber.Create(v));

        builder.Property(x => x.WorkflowVersionCtrlNbr)
            .HasConversion(x => x.Value, v => ControlNumber.Create(v));

        builder.Property(x => x.RailroadCtrlNbr)
            .HasConversion(x => x.Value, v => ControlNumber.Create(v));

        builder.Property(x => x.AggregateCtrlNbr)
            .HasConversion(
                x => x == null ? (long?)null : x.Value,
                v => v == null ? null : ControlNumber.Create(v.Value));

        builder.Property(x => x.TriggerTypeCtrlNbr)
            .HasConversion(
                x => x == null ? (long?)null : x.Value,
                v => v == null ? null : ControlNumber.Create(v.Value));

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.DetailsJson)
            .HasColumnType("TEXT")
            .IsRequired(false);

        builder.HasIndex(x => new { x.WorkflowTemplateCtrlNbr, x.StartedAtUtc });
        builder.HasIndex(x => x.WorkflowVersionCtrlNbr);
        builder.HasIndex(x => x.RailroadCtrlNbr);

        builder.HasOne<WorkflowTemplate>()
            .WithMany()
            .HasForeignKey(x => x.WorkflowTemplateCtrlNbr)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<WorkflowVersion>()
            .WithMany()
            .HasForeignKey(x => x.WorkflowVersionCtrlNbr)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<WorkflowTriggerType>()
            .WithMany()
            .HasForeignKey(x => x.TriggerTypeCtrlNbr)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(c => c.CreatedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });

        builder.OwnsOne(c => c.ModifiedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });

        builder.OwnsOne(c => c.DeletedBy, a =>
        {
            a.Property(x => x.AuditName)
                .HasConversion(n => n.Value, v => Name.Create(v))
                .HasMaxLength(50);
        });
    }
}
