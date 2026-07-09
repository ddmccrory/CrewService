using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace CrewService.UnitTests.SeniorityOps;

/// <summary>
/// End-to-end tests for the employee-wide off-property cascade in
/// <see cref="Application.SeniorityOps.SeniorityAppService.UpdateAsync"/>. Proves that moving a
/// single seniority record into a <see cref="StateType.OffProperty"/> state fans out across every
/// seniority record the employee holds (end-dating each one), deletes their individual
/// qualifications, and administratively cancels every live certification while leaving already
/// terminal certifications untouched. These run against a real orchestration UoW on a shared SQLite
/// connection so the sequential (non-nested) transaction flow is exercised.
/// </summary>
public sealed class SeniorityOffPropertyCascadeTests : IDisposable
{
    private readonly SeniorityVacancyTestHost _host = new();
    public void Dispose() => _host.Dispose();

    /// <summary>The seeded graph the cascade reads and mutates.</summary>
    private sealed record Fixture(
        ControlNumber EmployeeCtrlNbr,
        ControlNumber PrimarySeniorityCtrlNbr,
        ControlNumber SecondarySeniorityCtrlNbr,
        ControlNumber ActiveStateCtrlNbr,
        ControlNumber OffPropertyStateCtrlNbr,
        ControlNumber QualificationCtrlNbr,
        ControlNumber LiveCertificationCtrlNbr,
        ControlNumber RevokedCertificationCtrlNbr);

    /// <summary>
    /// Seeds an employee holding two seniority records (on two rosters), one manually granted
    /// qualification, and two certifications: one live (Active) and one already Revoked. An Active
    /// and an OffProperty seniority state are seeded so the update can flip from one to the other.
    /// No vacancy config is seeded, so the post-commit vacancy action is a no-op and the test
    /// isolates the cascade itself.
    /// </summary>
    private async Task<Fixture> SeedAsync(CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();

        var parent = Parent.Create("Test Parent");
        ctx.Parents.Add(parent);
        await ctx.SaveChangesAsync(ct);

        var railroadType = GroupType.Create("Railroad", null, true);
        ctx.Set<GroupType>().Add(railroadType);
        await ctx.SaveChangesAsync(ct);

        var workArea = DynamicGroup.Create(
            railroadType.CtrlNbr, "Test Work Area", null, null, true, "WA",
            parentCtrlNbr: parent.CtrlNbr);
        ctx.DynamicGroups.Add(workArea);
        await ctx.SaveChangesAsync(ct);

        var craft = Craft.Create(null, workArea.CtrlNbr, "Engineer", "Engineers", 1, false, false, 0, 0, 0, 0, 0, false, false, false, 0);
        ctx.Crafts.Add(craft);
        await ctx.SaveChangesAsync(ct);

        var primaryRoster = Roster.Create(craft.CtrlNbr, workArea.CtrlNbr, null, "Primary Roster", "Primary Rosters", 1);
        var secondaryRoster = Roster.Create(craft.CtrlNbr, workArea.CtrlNbr, null, "Secondary Roster", "Secondary Rosters", 2);
        ctx.Rosters.Add(primaryRoster);
        ctx.Rosters.Add(secondaryRoster);

        var empStatus = EmploymentStatus.Create(workArea.CtrlNbr, "ACT", "Active", 1, "A");
        ctx.EmploymentStatuses.Add(empStatus);
        await ctx.SaveChangesAsync(ct);

        var employee = Employee.Create(
            workArea.CtrlNbr, "jdoe", "E001", "000-00-0001", Gender.Male, Race.White,
            new DateTime(1990, 1, 1), DateTime.UtcNow, empStatus.CtrlNbr, "jdoe@example.com", "admin", "Admin User");
        ctx.Employees.Add(employee);

        var activeState = SeniorityState.Create("Active", StateType.Active, parent.CtrlNbr.Value);
        var offPropertyState = SeniorityState.Create("Terminated", StateType.OffProperty, parent.CtrlNbr.Value);
        ctx.Set<SeniorityState>().Add(activeState);
        ctx.Set<SeniorityState>().Add(offPropertyState);
        await ctx.SaveChangesAsync(ct);

        var primarySeniority = Domain.Models.Seniority.Seniority.Create(
            primaryRoster.CtrlNbr, employee.CtrlNbr, true, DateTime.UtcNow.AddYears(-5), 1, activeState.CtrlNbr, false);
        var secondarySeniority = Domain.Models.Seniority.Seniority.Create(
            secondaryRoster.CtrlNbr, employee.CtrlNbr, false, DateTime.UtcNow.AddYears(-3), 2, activeState.CtrlNbr, false);
        ctx.Set<Domain.Models.Seniority.Seniority>().Add(primarySeniority);
        ctx.Set<Domain.Models.Seniority.Seniority>().Add(secondarySeniority);
        await ctx.SaveChangesAsync(ct);

        var qualType = QualificationType.Create(parent.CtrlNbr, "CDL", "Commercial Driver License");
        ctx.Set<QualificationType>().Add(qualType);
        await ctx.SaveChangesAsync(ct);

        var qualification = EmployeeQualification.Create(
            employee.CtrlNbr, qualType.CtrlNbr, "admin", achievedAtUtc: DateTime.UtcNow.AddYears(-1));
        ctx.Set<EmployeeQualification>().Add(qualification);

        var regulatoryQual = RegulatoryQualification.Create(
            "ENGCERT", "240", "Locomotive Engineer Certification", true, 36,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)));
        ctx.Set<RegulatoryQualification>().Add(regulatoryQual);
        await ctx.SaveChangesAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var liveCertification = EmployeeCertification.Create(
            employee.CtrlNbr, regulatoryQual.CtrlNbr, "Engineer", today, 36);
        // Satisfy every enforced default check with a fresh passing result so the cert computes Active.
        foreach (var (checkType, _, stalenessLimitDays, isEnforced, _) in CertificationCheckDefaults.Checks)
        {
            if (isEnforced)
                liveCertification.AddEligibilityCheck(checkType, today, stalenessLimitDays, "Pass", "Examiner");
        }

        var revokedCertification = EmployeeCertification.Create(
            employee.CtrlNbr, regulatoryQual.CtrlNbr, "Conductor", today, 36);
        revokedCertification.Revoke(DateTime.UtcNow.AddYears(1));

        ctx.Set<EmployeeCertification>().Add(liveCertification);
        ctx.Set<EmployeeCertification>().Add(revokedCertification);
        await ctx.SaveChangesAsync(ct);

        return new Fixture(
            employee.CtrlNbr, primarySeniority.CtrlNbr, secondarySeniority.CtrlNbr,
            activeState.CtrlNbr, offPropertyState.CtrlNbr, qualification.CtrlNbr,
            liveCertification.CtrlNbr, revokedCertification.CtrlNbr);
    }

    private async Task<Domain.Models.Seniority.Seniority?> GetSeniorityAsync(ControlNumber ctrlNbr, CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();
        return await ctx.Set<Domain.Models.Seniority.Seniority>()
            .SingleOrDefaultAsync(s => s.CtrlNbr == ctrlNbr, ct);
    }

    private async Task<EmployeeCertification?> GetCertificationAsync(ControlNumber ctrlNbr, CancellationToken ct)
    {
        await using var ctx = _host.CreateReadContext();
        return await ctx.Set<EmployeeCertification>()
            .SingleOrDefaultAsync(c => c.CtrlNbr == ctrlNbr, ct);
    }

    [Fact]
    public async Task Update_ToOffProperty_EndDatesEverySeniorityRecordForEmployee()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedAsync(ct);

        await _host.Seniority.UpdateAsync(
            f.PrimarySeniorityCtrlNbr, lastActiveRoster: true, rosterDate: DateTime.UtcNow.AddYears(-5),
            rank: 1, seniorityStateCtrlNbr: f.OffPropertyStateCtrlNbr, canTrain: false, ct);

        // The updated record and the OTHER record the employee holds are both moved off property
        // and end-dated — the cascade is employee-wide, not scoped to the record passed in.
        var primary = await GetSeniorityAsync(f.PrimarySeniorityCtrlNbr, ct);
        var secondary = await GetSeniorityAsync(f.SecondarySeniorityCtrlNbr, ct);

        Assert.NotNull(primary);
        Assert.NotNull(secondary);
        Assert.Equal(f.OffPropertyStateCtrlNbr, primary!.SeniorityStateCtrlNbr);
        Assert.Equal(f.OffPropertyStateCtrlNbr, secondary!.SeniorityStateCtrlNbr);
        Assert.NotNull(primary.SeniorityEndDate);
        Assert.NotNull(secondary.SeniorityEndDate);
    }

    [Fact]
    public async Task Update_ToOffProperty_RemovesIndividualQualifications()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedAsync(ct);

        await _host.Seniority.UpdateAsync(
            f.PrimarySeniorityCtrlNbr, lastActiveRoster: true, rosterDate: DateTime.UtcNow.AddYears(-5),
            rank: 1, seniorityStateCtrlNbr: f.OffPropertyStateCtrlNbr, canTrain: false, ct);

        // The qualification is soft-deleted, so the global soft-delete query filter hides it from a
        // normal read while it still exists when query filters are ignored.
        await using var ctx = _host.CreateReadContext();
        var visible = await ctx.Set<EmployeeQualification>()
            .SingleOrDefaultAsync(q => q.CtrlNbr == f.QualificationCtrlNbr, ct);
        var includingDeleted = await ctx.Set<EmployeeQualification>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(q => q.CtrlNbr == f.QualificationCtrlNbr, ct);

        Assert.Null(visible);
        Assert.NotNull(includingDeleted);
        Assert.True(includingDeleted!.IsDeleted);
    }

    [Fact]
    public async Task Update_ToOffProperty_CancelsLiveCertificationsButLeavesTerminalOnesUntouched()
    {
        var ct = TestContext.Current.CancellationToken;
        var f = await SeedAsync(ct);

        // Sanity: the live certification computed to Active during seeding.
        var before = await GetCertificationAsync(f.LiveCertificationCtrlNbr, ct);
        Assert.NotNull(before);
        Assert.Equal(CertificationStatuses.Active, before!.Status);

        await _host.Seniority.UpdateAsync(
            f.PrimarySeniorityCtrlNbr, lastActiveRoster: true, rosterDate: DateTime.UtcNow.AddYears(-5),
            rank: 1, seniorityStateCtrlNbr: f.OffPropertyStateCtrlNbr, canTrain: false, ct);

        // The live certification is administratively cancelled with the off-property reason.
        var live = await GetCertificationAsync(f.LiveCertificationCtrlNbr, ct);
        Assert.NotNull(live);
        Assert.Equal(CertificationStatuses.Cancelled, live!.Status);
        Assert.NotNull(live.CancelledAtUtc);
        Assert.Equal("Employee off property", live.CancellationReason);

        // The already-revoked certification is a terminal FRA due-process outcome and must not be
        // overwritten by the administrative cancellation.
        var revoked = await GetCertificationAsync(f.RevokedCertificationCtrlNbr, ct);
        Assert.NotNull(revoked);
        Assert.Equal(CertificationStatuses.Revoked, revoked!.Status);
        Assert.Null(revoked.CancelledAtUtc);
    }
}