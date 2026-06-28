using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.VacancyCalls;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrewService.Persistance.Configurations;

internal class VacancyCallRequestConfiguration : IEntityTypeConfiguration<VacancyCallRequest>
{
    public void Configure(EntityTypeBuilder<VacancyCallRequest> builder)
    {
        builder.HasKey(n => n.CtrlNbr);
        builder.Property(n => n.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(n => n.PositionSlotCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(n => n.EmployeeCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(n => n.TemplateType).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Status).HasMaxLength(20).IsRequired();
        builder.Property(n => n.ExternalId).HasMaxLength(200);

        builder.HasMany(n => n.Responses).WithOne().HasForeignKey(r => r.VacancyCallRequestCtrlNbr).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<PositionSlot>().WithMany().HasForeignKey(n => n.PositionSlotCtrlNbr).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(n => n.EmployeeCtrlNbr).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(n => n.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(n => n.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(n => n.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}

internal class VacancyCallResponseConfiguration : IEntityTypeConfiguration<VacancyCallResponse>
{
    public void Configure(EntityTypeBuilder<VacancyCallResponse> builder)
    {
        builder.HasKey(r => r.CtrlNbr);
        builder.Property(r => r.CtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.VacancyCallRequestCtrlNbr).HasConversion(c => c.Value, v => ControlNumber.Create(v));
        builder.Property(r => r.ResponseType).HasMaxLength(20).IsRequired();
        builder.Property(r => r.DeviceType).HasMaxLength(50);

        builder.OwnsOne(r => r.CreatedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.ModifiedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
        builder.OwnsOne(r => r.DeletedBy, a => { a.Property(x => x.AuditName).HasConversion(n => n.Value, v => Name.Create(v)).HasMaxLength(50); });
    }
}
