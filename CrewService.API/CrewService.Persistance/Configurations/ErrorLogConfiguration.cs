using CrewService.Domain.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal sealed class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> builder)
    {
        builder.ToTable("ErrorLogs");

        builder.HasKey(e => e.ErrorId);

        builder.Property(e => e.ErrorId)
            .ValueGeneratedNever();

        builder.Property(e => e.OccurredAtUtc)
            .IsRequired();

        builder.Property(e => e.FirstOccurredAtUtc)
            .IsRequired();

        builder.Property(e => e.LastOccurredAtUtc)
            .IsRequired();

        builder.Property(e => e.LoggedAtUtc)
            .IsRequired();

        builder.Property(e => e.ErrorKind)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.SourceApp)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.SourceLayer)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.Severity)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.FingerprintHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.OccurrenceCount)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.ResolvedBy)
            .HasMaxLength(100);

        builder.Property(e => e.SuppressionReason)
            .HasMaxLength(1000);

        builder.Property(e => e.ErrorCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ExceptionType)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(e => e.Message)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(e => e.TraceId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.Route)
            .HasMaxLength(512);

        builder.Property(e => e.Method)
            .HasMaxLength(256);

        builder.Property(e => e.PerformedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => e.OccurredAtUtc);
        builder.HasIndex(e => e.ParentCtrlNbr);
        builder.HasIndex(e => e.RailroadCtrlNbr);
        builder.HasIndex(e => e.Severity);
        builder.HasIndex(e => e.SourceApp);
        builder.HasIndex(e => e.TraceId);
        builder.HasIndex(e => e.ErrorKind);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.FingerprintHash);
        builder.HasIndex(e => new { e.FingerprintHash, e.Status });
    }
}
