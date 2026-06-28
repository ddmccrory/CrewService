using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class PendingSeniorityStateChangeConfiguration : IEntityTypeConfiguration<PendingSeniorityStateChange>
{
    public void Configure(EntityTypeBuilder<PendingSeniorityStateChange> builder)
    {
        builder.HasKey(p => p.CtrlNbr);

        builder.Property(p => p.CtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v));

        builder.Property(p => p.SeniorityCtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v)).IsRequired();

        builder.Property(p => p.EmployeeCtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v)).IsRequired();

        builder.Property(p => p.FromSeniorityStateCtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v)).IsRequired();

        builder.Property(p => p.ToSeniorityStateCtrlNbr).HasConversion(
            c => c.Value, v => ControlNumber.Create(v)).IsRequired();

        builder.Property(p => p.EffectiveDateUtc).IsRequired();
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .IsRequired();
        builder.Property(p => p.ScheduledByUserId).IsRequired().HasMaxLength(256);
        builder.Property(p => p.ScheduledAtUtc).IsRequired();
        builder.Property(p => p.ProcessedAtUtc);
        builder.Property(p => p.CancelledByUserId).HasMaxLength(256);

        // Enforce one pending change per employee at the DB level via filtered unique index.
        builder.HasIndex(p => p.EmployeeCtrlNbr)
            .IsUnique()
            .HasFilter("[Status] = 'Pending'")
            .HasDatabaseName("UIX_PendingSeniorityStateChange_Employee_Pending");

        // Index for efficient due-time queries.
        builder.HasIndex(p => new { p.Status, p.EffectiveDateUtc })
            .HasDatabaseName("IX_PendingSeniorityStateChange_Status_EffectiveDate");

        builder.HasOne<Domain.Models.Seniority.Seniority>()
            .WithMany()
            .HasForeignKey(p => p.SeniorityCtrlNbr)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SeniorityState>()
            .WithMany()
            .HasForeignKey(p => p.FromSeniorityStateCtrlNbr)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SeniorityState>()
            .WithMany()
            .HasForeignKey(p => p.ToSeniorityStateCtrlNbr)
            .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(p => p.EmployeeCtrlNbr)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(p => p.CreatedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
        });

        builder.OwnsOne(p => p.ModifiedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
        });

        builder.OwnsOne(p => p.DeletedBy, audit =>
        {
            audit.Property(a => a.AuditName).HasConversion(
                name => name.Value,
                value => Name.Create(value));
        });
    }
}
