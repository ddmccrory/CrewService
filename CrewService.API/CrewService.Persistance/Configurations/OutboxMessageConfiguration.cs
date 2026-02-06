using CrewService.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.MessageId);

        builder.Property(o => o.MessageId)
            .ValueGeneratedNever();

        builder.Property(o => o.EventType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(o => o.AggregateType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(o => o.AggregateId)
            .IsRequired();

        builder.Property(o => o.PayloadJson)
            .IsRequired();

        builder.Property(o => o.CorrelationId)
            .HasMaxLength(128);

        builder.Property(o => o.OrchestrationId)
            .HasMaxLength(128);

        builder.Property(o => o.IdempotencyKey)
            .HasMaxLength(256);

        builder.Property(o => o.EventVersion)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.PublishedAt);

        builder.Property(o => o.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(o => o.Retries)
            .IsRequired()
            .HasDefaultValue(0);

        // Index for efficient polling by background publisher
        builder.HasIndex(o => new { o.Status, o.CreatedAt })
            .HasDatabaseName("IX_OutboxMessages_Status_CreatedAt");

        // Unique constraint on IdempotencyKey to prevent duplicate inserts
        builder.HasIndex(o => o.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("IX_OutboxMessages_IdempotencyKey");

        // Index for correlation/orchestration queries
        builder.HasIndex(o => o.CorrelationId)
            .HasDatabaseName("IX_OutboxMessages_CorrelationId");

        builder.HasIndex(o => o.OrchestrationId)
            .HasDatabaseName("IX_OutboxMessages_OrchestrationId");
    }
}