using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CrewService.UnitTests.Persistence;

public class CascadingSoftDeleteTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private static async Task<DynamicGroup> CreateGroupAsync(CrewServiceDbContext ctx, CancellationToken ct)
    {
        var groupType = GroupType.Create("TestType", null, true);
        ctx.Set<GroupType>().Add(groupType);
        await ctx.SaveChangesAsync(ct);
        var group = DynamicGroup.Create(groupType.CtrlNbr.Value, "Test Group", null, null, true, "TST");
        ctx.DynamicGroups.Add(group);
        await ctx.SaveChangesAsync(ct);
        return group;
    }

    [Fact]
    public async Task SoftDelete_Crew_CascadesToCrewAssignments()
    {
        var ct = TestContext.Current.CancellationToken;
        using var ctx = _factory.CreateContext();
        var group = await CreateGroupAsync(ctx, ct);
        var assignment = Assignment.Create(group.CtrlNbr, "A1", "Assignment 1");
        ctx.Assignments.Add(assignment);
        var crew = Crew.Create("REGULAR", group.CtrlNbr, "Test Crew");
        ctx.Crews.Add(crew);
        await ctx.SaveChangesAsync(ct);
        var ca = CrewAssignment.Create(crew.CtrlNbr, assignment.CtrlNbr, 62, DateTime.UtcNow);
        ctx.CrewAssignments.Add(ca);
        await ctx.SaveChangesAsync(ct);

        crew.SoftDelete("test-user");
        ctx.Crews.Update(crew);
        await ctx.SaveChangesAsync(ct);

        ctx.ChangeTracker.Clear();
        Assert.Empty(await ctx.CrewAssignments.Where(a => a.CrewCtrlNbr == crew.CtrlNbr).ToListAsync(ct));
        var all = await ctx.CrewAssignments.IgnoreQueryFilters().Where(a => a.CrewCtrlNbr == crew.CtrlNbr).ToListAsync(ct);
        Assert.Single(all);
        Assert.True(all[0].IsDeleted);
    }

    [Fact]
    public async Task SoftDelete_Crew_CascadesToCrewPositions()
    {
        var ct = TestContext.Current.CancellationToken;
        using var ctx = _factory.CreateContext();
        var group = await CreateGroupAsync(ctx, ct);
        var craft = Craft.Create(null, group.CtrlNbr, "Engineer", "Engineers", 1, false, false, 0, 0, 0, 0, 0, false, false, false, 0);
        ctx.Crafts.Add(craft);
        await ctx.SaveChangesAsync(ct);
        var role = CraftRole.Create(craft.CtrlNbr, "ENGR", "Engineer");
        ctx.Set<CraftRole>().Add(role);
        await ctx.SaveChangesAsync(ct);
        var crew = Crew.Create("REGULAR", group.CtrlNbr, "Test Crew");
        ctx.Crews.Add(crew);
        var sp = StaffablePosition.Create(StaffablePositionType.Crew);
        ctx.StaffablePositions.Add(sp);
        await ctx.SaveChangesAsync(ct);
        var pos = CrewPosition.Create(crew.CtrlNbr, role.CtrlNbr, 1, sp.CtrlNbr);
        ctx.CrewPositions.Add(pos);
        await ctx.SaveChangesAsync(ct);

        crew.SoftDelete("test-user");
        ctx.Crews.Update(crew);
        await ctx.SaveChangesAsync(ct);

        ctx.ChangeTracker.Clear();
        Assert.Empty(await ctx.CrewPositions.Where(p => p.CrewCtrlNbr == crew.CtrlNbr).ToListAsync(ct));
        var all = await ctx.CrewPositions.IgnoreQueryFilters().Where(p => p.CrewCtrlNbr == crew.CtrlNbr).ToListAsync(ct);
        Assert.Single(all);
        Assert.True(all[0].IsDeleted);
    }

    [Fact]
    public async Task SoftDelete_Crew_CascadesRecursively_CrewPosition_To_CrewIncumbency()
    {
        var ct = TestContext.Current.CancellationToken;
        using var ctx = _factory.CreateContext();
        var group = await CreateGroupAsync(ctx, ct);
        var craft = Craft.Create(null, group.CtrlNbr, "Engineer", "Engineers", 1, false, false, 0, 0, 0, 0, 0, false, false, false, 0);
        ctx.Crafts.Add(craft);
        await ctx.SaveChangesAsync(ct);
        var role = CraftRole.Create(craft.CtrlNbr, "ENGR", "Engineer");
        ctx.Set<CraftRole>().Add(role);
        var empStatus = EmploymentStatus.Create(group.CtrlNbr, "ACT", "Active", 1, "A");
        ctx.EmploymentStatuses.Add(empStatus);
        await ctx.SaveChangesAsync(ct);
        var employee = Employee.Create(group.CtrlNbr, "jdoe", "E001", "000-00-0001", Gender.Male, Race.White, new DateTime(1990, 1, 1), DateTime.UtcNow, empStatus.CtrlNbr, "jdoe@example.com", "admin", "Admin User");
        ctx.Employees.Add(employee);
        var crew = Crew.Create("REGULAR", group.CtrlNbr, "Test Crew");
        ctx.Crews.Add(crew);
        var sp = StaffablePosition.Create(StaffablePositionType.Crew);
        ctx.StaffablePositions.Add(sp);
        await ctx.SaveChangesAsync(ct);
        var pos = CrewPosition.Create(crew.CtrlNbr, role.CtrlNbr, 1, sp.CtrlNbr);
        ctx.CrewPositions.Add(pos);
        await ctx.SaveChangesAsync(ct);
        var incumbency = CrewIncumbency.Create(pos.CtrlNbr, employee.CtrlNbr, DateTime.UtcNow);
        ctx.Set<CrewIncumbency>().Add(incumbency);
        await ctx.SaveChangesAsync(ct);

        crew.SoftDelete("test-user");
        ctx.Crews.Update(crew);
        await ctx.SaveChangesAsync(ct);

        ctx.ChangeTracker.Clear();
        Assert.Empty(await ctx.CrewPositions.Where(p => p.CrewCtrlNbr == crew.CtrlNbr).ToListAsync(ct));
        Assert.Empty(await ctx.Set<CrewIncumbency>().Where(i => i.CrewPositionCtrlNbr == pos.CtrlNbr).ToListAsync(ct));
        var allPos = await ctx.CrewPositions.IgnoreQueryFilters().Where(p => p.CrewCtrlNbr == crew.CtrlNbr).ToListAsync(ct);
        Assert.Single(allPos);
        Assert.True(allPos[0].IsDeleted);
        var allInc = await ctx.Set<CrewIncumbency>().IgnoreQueryFilters().Where(i => i.CrewPositionCtrlNbr == pos.CtrlNbr).ToListAsync(ct);
        Assert.Single(allInc);
        Assert.True(allInc[0].IsDeleted);
    }
}