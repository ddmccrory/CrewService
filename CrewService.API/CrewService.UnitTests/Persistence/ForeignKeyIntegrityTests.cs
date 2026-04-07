using CrewService.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CrewService.UnitTests.Persistence;

public class ForeignKeyIntegrityTests
{
    [Fact]
    public void AllForeignKeys_HaveExplicitDeleteBehavior()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var foreignKeys = context.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => !fk.IsOwnership)
            .ToList();

        Assert.NotEmpty(foreignKeys);

        foreach (var fk in foreignKeys)
        {
            var declaring = fk.DeclaringEntityType.ShortName();
            var principal = fk.PrincipalEntityType.ShortName();
            var props = string.Join(", ", fk.Properties.Select(p => p.Name));

            Assert.True(
                fk.DeleteBehavior is DeleteBehavior.Restrict
                    or DeleteBehavior.Cascade
                    or DeleteBehavior.SetNull,
                $"FK {declaring}({props}) → {principal} uses disallowed " +
                $"DeleteBehavior.{fk.DeleteBehavior}. " +
                $"Expected Restrict, Cascade, or SetNull.");
        }
    }

    [Fact]
    public void NoCtrlNbrProperty_IsOrphanedWithoutForeignKey()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        // Known polymorphic or external FK columns that intentionally lack a relationship
        var polymorphicExclusions = new HashSet<(string Entity, string Property)>
        {
            // Polymorphic — TargetCtrlNbr can reference different entity types
            ("AbolishmentRecord", "TargetCtrlNbr"),
            ("PositionVacancy", "TargetCtrlNbr"),
            ("SeniorityMove", "TargetPositionCtrlNbr"),
            // Polymorphic — AssignmentSourceCtrlNbr can reference SeniorityMove, Bulletin, etc.
            ("PositionAssignment", "AssignmentSourceCtrlNbr"),
            // Multi-tenant — Client entity lives outside this bounded context
            ("AddressType", "ClientCtrlNbr"),
            ("EmailAddressType", "ClientCtrlNbr"),
            ("PhoneNumberType", "ClientCtrlNbr"),
            ("Employee", "ClientCtrlNbr"),
            ("EmploymentStatus", "ClientCtrlNbr"),
            // External — RailroadPayrollDepartment entity not in this bounded context
            ("Roster", "RailroadPayrollDepartmentCtrlNbr"),
            // Scoping — 0 means "universal"; these are scope filters, not FK relationships
            ("GroupType", "ParentCtrlNbr"),
            ("GroupType", "RailroadCtrlNbr"),
            ("GroupType", "ParentGroupTypeCtrlNbr"),
            // Permission.ParentCtrlNbr is a scope filter for per-parent overrides, not a FK
            ("Permission", "ParentCtrlNbr"),
            // Hierarchical scoping — ParentCtrlNbr references Parent entity (outside bounded context)
            ("Department", "ParentCtrlNbr"),
            ("Craft", "ParentCtrlNbr"),
            // Scoping — DynamicGroup scope filters (0 = universal)
            ("DynamicGroup", "ParentCtrlNbr"),
            ("DynamicGroup", "RailroadCtrlNbr"),
            // Snapshot — denormalized point-in-time copies on call sheet entities
            ("ShiftInstance", "DepartmentCtrlNbr"),
            ("ShiftInstance", "ShiftDefinitionCtrlNbr"),
            ("PositionSlotInstance", "AssignmentCtrlNbr"),
            ("AssignmentNote", "AssignmentCtrlNbr"),
        };

        var orphans = new List<string>();

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            if (entityType.IsOwned()) continue;

            var fkPropertyNames = entityType.GetForeignKeys()
                .SelectMany(fk => fk.Properties)
                .Select(p => p.Name)
                .ToHashSet();

            var ctrlNbrProperties = entityType.GetProperties()
                .Where(p => p.Name.EndsWith("CtrlNbr") && p.Name != "CtrlNbr")
                .Where(p => !fkPropertyNames.Contains(p.Name));

            foreach (var prop in ctrlNbrProperties)
            {
                var key = (entityType.ShortName(), prop.Name);
                if (!polymorphicExclusions.Contains(key))
                {
                    orphans.Add($"{entityType.ShortName()}.{prop.Name}");
                }
            }
        }

        Assert.True(
            orphans.Count == 0,
            $"The following CtrlNbr properties lack FK relationships:\n" +
            string.Join("\n", orphans.Select(o => $"  - {o}")));
    }

    [Theory]
    [InlineData("CrewPosition", "Crew", DeleteBehavior.Cascade)]
    [InlineData("CrewIncumbency", "CrewPosition", DeleteBehavior.Cascade)]
    [InlineData("BoardMember", "ExtraBoard", DeleteBehavior.Cascade)]
    [InlineData("BulletinBid", "Bulletin", DeleteBehavior.Cascade)]
    [InlineData("DisplacementClaim", "DisplacementCase", DeleteBehavior.Cascade)]
    [InlineData("OffDutyRecord", "OnDutyRecord", DeleteBehavior.Cascade)]
    [InlineData("OnDutyBillingRecord", "OnDutyRecord", DeleteBehavior.Cascade)]
    [InlineData("PayrollRecord", "PayrollRun", DeleteBehavior.Cascade)]
    [InlineData("HolidayQualificationRule", "Holiday", DeleteBehavior.Cascade)]
    [InlineData("HolidayPayrollRecord", "Holiday", DeleteBehavior.Cascade)]
    [InlineData("RailroadInformationReadReceipt", "RailroadInformation", DeleteBehavior.Cascade)]
    [InlineData("WorkerExecutionLog", "WorkerSchedule", DeleteBehavior.Cascade)]
    [InlineData("DrugAlcoholAction", "DrugAlcoholTestRecord", DeleteBehavior.Cascade)]
    [InlineData("SafetyObservationResolution", "SafetyObservation", DeleteBehavior.Cascade)]
    [InlineData("CertificationRevocationRecord", "EmployeeCertification", DeleteBehavior.Cascade)]
    [InlineData("CrewAssignment", "Crew", DeleteBehavior.Cascade)]
    [InlineData("PayrollExportBatch", "PayrollRun", DeleteBehavior.Cascade)]
    [InlineData("AbsenceCodeCraftOverride", "AbsenceCode", DeleteBehavior.Cascade)]
    [InlineData("AbsenceApproval", "AbsenceRequest", DeleteBehavior.Cascade)]
    [InlineData("AbsenceMarkUp", "AbsenceRequest", DeleteBehavior.Cascade)]
    [InlineData("CertificationEligibilityCheck", "EmployeeCertification", DeleteBehavior.Cascade)]
    [InlineData("FraDutyTourSegment", "FraDutyTour", DeleteBehavior.Cascade)]
    [InlineData("FraTransportationSegment", "FraDutyTour", DeleteBehavior.Cascade)]
    [InlineData("FraOtherServiceSegment", "FraDutyTour", DeleteBehavior.Cascade)]
    [InlineData("NotificationResponse", "NotificationRequest", DeleteBehavior.Cascade)]
    [InlineData("RosterBoardPosition", "RosterBoard", DeleteBehavior.Cascade)]
    [InlineData("SafetyObservationAction", "SafetyObservation", DeleteBehavior.Cascade)]
    [InlineData("EarningApproval", "PayrollRecord", DeleteBehavior.Cascade)]
    public void AggregateChild_CascadesFromParent(
        string childEntity, string parentEntity, DeleteBehavior expected)
    {
        AssertForeignKey(childEntity, parentEntity, expected);
    }

    [Theory]
    [InlineData("CrewIncumbency", "Employee", DeleteBehavior.Restrict)]
    [InlineData("CrewPosition", "StaffablePosition", DeleteBehavior.Restrict)]
    [InlineData("RosterBoardPosition", "StaffablePosition", DeleteBehavior.Restrict)]
    [InlineData("PositionAssignment", "StaffablePosition", DeleteBehavior.Restrict)]
    [InlineData("PositionAssignment", "Employee", DeleteBehavior.Restrict)]
    [InlineData("CrewPosition", "CraftRole", DeleteBehavior.Restrict)]
    [InlineData("BoardMember", "Employee", DeleteBehavior.Restrict)]
    [InlineData("ExtraBoard", "Craft", DeleteBehavior.Restrict)]
    [InlineData("Seniority", "Employee", DeleteBehavior.Restrict)]
    [InlineData("Seniority", "Roster", DeleteBehavior.Restrict)]
    [InlineData("Roster", "Craft", DeleteBehavior.Restrict)]
    [InlineData("Craft", "DynamicGroup", DeleteBehavior.Restrict)]
    [InlineData("Craft", "RegulatoryStandard", DeleteBehavior.Restrict)]
    [InlineData("CraftRole", "Craft", DeleteBehavior.Restrict)]
    [InlineData("PositionSlot", "WorkInstance", DeleteBehavior.Restrict)]
    [InlineData("PositionSlot", "Employee", DeleteBehavior.Restrict)]
    [InlineData("PositionSlotInstance", "Employee", DeleteBehavior.Restrict)]

    [InlineData("FraDutyTour", "Employee", DeleteBehavior.Restrict)]
    [InlineData("FraDutyTour", "RegulatoryStandard", DeleteBehavior.Restrict)]
    [InlineData("PayrollRecord", "Employee", DeleteBehavior.Restrict)]
    [InlineData("PayrollRecord", "OnDutyRecord", DeleteBehavior.Restrict)]
    [InlineData("PayRate", "CraftRole", DeleteBehavior.Restrict)]
    [InlineData("UserParentAssignment", "Parent", DeleteBehavior.Restrict)]
    [InlineData("Invitation", "Parent", DeleteBehavior.Restrict)]
    [InlineData("Employee", "EmploymentStatus", DeleteBehavior.Restrict)]
    [InlineData("DisplacementCase", "Employee", DeleteBehavior.Restrict)]
    [InlineData("DisplacementCase", "Craft", DeleteBehavior.Restrict)]
    [InlineData("Crew", "DynamicGroup", DeleteBehavior.Restrict)]
    [InlineData("Address", "AddressType", DeleteBehavior.Restrict)]
    [InlineData("EmailAddress", "EmailAddressType", DeleteBehavior.Restrict)]
    [InlineData("PhoneNumber", "PhoneNumberType", DeleteBehavior.Restrict)]
    [InlineData("EmployeeBooking", "PositionSlot", DeleteBehavior.Restrict)]
    [InlineData("OnDutyRecord", "EmployeeBooking", DeleteBehavior.Restrict)]
    [InlineData("CertificationRevocationRecord", "Employee", DeleteBehavior.Restrict)]
    [InlineData("HolidayPayrollRecord", "PayrollRecord", DeleteBehavior.Restrict)]
    [InlineData("HolidayQualificationRule", "Craft", DeleteBehavior.Restrict)]
    [InlineData("PayrollImportRecord", "PayrollRecord", DeleteBehavior.Restrict)]
    [InlineData("SlotRequirement", "CraftRole", DeleteBehavior.Restrict)]
    [InlineData("SlotRequirement", "RegulatoryQualification", DeleteBehavior.Restrict)]

    public void CrossAggregate_RestrictsDelete(
        string childEntity, string parentEntity, DeleteBehavior expected)
    {
        AssertForeignKey(childEntity, parentEntity, expected);
    }

    [Theory]
    [InlineData("DynamicGroup", "DynamicGroup", DeleteBehavior.SetNull)]
    [InlineData("TimeEntry", "TimeEntry", DeleteBehavior.SetNull)]
    public void SelfReferentialForeignKey_SetsNull(
        string childEntity, string parentEntity, DeleteBehavior expected)
    {
        AssertForeignKey(childEntity, parentEntity, expected);
    }

    /// <summary>
    /// Validates entities with multiple FKs to the same parent entity type.
    /// </summary>
    [Theory]
    [InlineData("SeniorityMove", "Employee", "EmployeeCtrlNbr", DeleteBehavior.Restrict)]
    [InlineData("SeniorityMove", "Employee", "DisplacedEmployeeCtrlNbr", DeleteBehavior.Restrict)]
    [InlineData("DispatchOverride", "Employee", "EmployeeCtrlNbr", DeleteBehavior.Restrict)]
    [InlineData("DispatchOverride", "Employee", "ApprovedByCtrlNbr", DeleteBehavior.Restrict)]
    [InlineData("AbsenceRequest", "Employee", "EmployeeCtrlNbr", DeleteBehavior.Restrict)]
    [InlineData("AbsenceRequest", "Employee", "ApprovedByCtrlNbr", DeleteBehavior.Restrict)]
    [InlineData("AbsenceRequest", "AbsenceCode", "AbsenceCodeCtrlNbr", DeleteBehavior.Restrict)]
    [InlineData("AbsenceRequest", "PositionSlot", "PositionSlotCtrlNbr", DeleteBehavior.Restrict)]
    [InlineData("DispatchProjection", "Employee", "ProjectedEmployeeCtrlNbr", DeleteBehavior.Restrict)]
    [InlineData("DispatchDecisionLog", "Employee", "SelectedEmployeeCtrlNbr", DeleteBehavior.Restrict)]
    [InlineData("PositionVacancy", "Employee", "PreviousIncumbentCtrlNbr", DeleteBehavior.Restrict)]
    [InlineData("Bulletin", "Employee", "AwardedEmployeeCtrlNbr", DeleteBehavior.Restrict)]
    [InlineData("TeamsWebhookConfig", "DynamicGroup", "RailroadCtrlNbr", DeleteBehavior.Restrict)]
    [InlineData("TeamsWebhookConfig", "DynamicGroup", "WorkAreaGroupCtrlNbr", DeleteBehavior.Restrict)]
    public void MultiForeignKey_HasCorrectDeleteBehavior(
        string childEntity, string parentEntity, string fkPropertyName, DeleteBehavior expected)
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var childType = context.Model.GetEntityTypes()
            .Single(e => e.ShortName() == childEntity);

        var fk = childType.GetForeignKeys()
            .SingleOrDefault(f => f.PrincipalEntityType.ShortName() == parentEntity
                && f.Properties.Any(p => p.Name == fkPropertyName));

        Assert.NotNull(fk);
        Assert.Equal(expected, fk!.DeleteBehavior);
    }

    private static void AssertForeignKey(string childEntity, string parentEntity, DeleteBehavior expected)
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var childType = context.Model.GetEntityTypes()
            .Single(e => e.ShortName() == childEntity);

        var fk = childType.GetForeignKeys()
            .SingleOrDefault(f => f.PrincipalEntityType.ShortName() == parentEntity);

        Assert.NotNull(fk);
        Assert.Equal(expected, fk!.DeleteBehavior);
    }
}
