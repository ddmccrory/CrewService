using CrewService.Domain.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class DomainEventLogConfiguration : IEntityTypeConfiguration<DomainEventLog>
{
    public void Configure(EntityTypeBuilder<DomainEventLog> builder)
    {
        builder.ToTable("DomainEventLogs");

        builder.HasKey(l => l.EventId);

        builder.Property(l => l.EventId)
            .ValueGeneratedNever();

        builder.Property(l => l.EventType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(l => l.AggregateType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(l => l.AggregateId)
            .IsRequired();

        builder.Property(l => l.OccurredAt)
            .IsRequired();

        builder.Property(l => l.PerformedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.LoggedAtUtc)
            .IsRequired();

        builder.HasIndex(l => l.AggregateType);
        builder.HasIndex(l => l.AggregateId);
        builder.HasIndex(l => l.EventType);
        builder.HasIndex(l => l.OccurredAt);
        builder.HasIndex(l => l.ParentCtrlNbr);
    }
}
