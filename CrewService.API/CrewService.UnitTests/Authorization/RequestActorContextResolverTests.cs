using CrewService.Application.Authorization;
using CrewService.Domain.Constants;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Xunit;

namespace CrewService.UnitTests.Authorization;

public sealed class RequestActorContextResolverTests
{
    [Fact]
    public async Task ResolveAsync_ResolvesLinkedEmployeeByUserId_WithoutUnitOfWorkFactory()
    {
        var employee = BuildEmployee(userId: "user-abc", employeeNumber: "E123");
        var repo = new FakeEmployeeRepository(employeeByUserId: employee, employeeByNumber: null);

        var httpContext = BuildHttpContext(
            new Claim(ClaimTypes.NameIdentifier, "user-abc"),
            new Claim(CustomClaimTypes.EmployeeNumber, "E123"));

        var resolver = new RequestActorContextResolver(new HttpContextAccessor { HttpContext = httpContext }, repo);
        var context = await resolver.ResolveAsync(requestedEmployeeCtrlNbr: employee.CtrlNbr.Value, ct: TestContext.Current.CancellationToken);

        Assert.Equal(employee.CtrlNbr.Value, context.CurrentEmployeeCtrlNbr);
        Assert.True(context.IsLinkedEmployee);
        Assert.True(context.IsSelfEmployeeContext);
        Assert.False(context.IsActingOnBehalfOfEmployee);
    }

    [Fact]
    public async Task ResolveAsync_FallsBackToEmployeeNumberClaim_WhenUserIdLookupMisses()
    {
        var employee = BuildEmployee(userId: "other-user", employeeNumber: "E999");
        var repo = new FakeEmployeeRepository(employeeByUserId: null, employeeByNumber: employee);

        var httpContext = BuildHttpContext(
            new Claim(ClaimTypes.NameIdentifier, "missing-user"),
            new Claim(CustomClaimTypes.EmployeeNumber, "E999"));

        var resolver = new RequestActorContextResolver(new HttpContextAccessor { HttpContext = httpContext }, repo);
        var context = await resolver.ResolveAsync(requestedEmployeeCtrlNbr: 123456, ct: TestContext.Current.CancellationToken);

        Assert.Equal(employee.CtrlNbr.Value, context.CurrentEmployeeCtrlNbr);
        Assert.True(context.IsLinkedEmployee);
        Assert.False(context.IsSelfEmployeeContext);
        Assert.True(context.IsActingOnBehalfOfEmployee);
    }

    [Fact]
    public async Task ResolveAsync_WithParentHeader_MapsParentCtrlNbr()
    {
        var repo = new FakeEmployeeRepository(employeeByUserId: null, employeeByNumber: null);
        var httpContext = BuildHttpContext(new Claim(ClaimTypes.NameIdentifier, "no-employee"));
        httpContext.Request.Headers["x-parent-ctrl-nbr"] = "77";

        var resolver = new RequestActorContextResolver(new HttpContextAccessor { HttpContext = httpContext }, repo);
        var context = await resolver.ResolveAsync(requestedEmployeeCtrlNbr: 10, ct: TestContext.Current.CancellationToken);

        Assert.Equal(77, context.ParentCtrlNbr);
        Assert.False(context.IsLinkedEmployee);
        Assert.True(context.IsActingOnBehalfOfEmployee);
    }

    private static Employee BuildEmployee(string userId, string employeeNumber)
        => Employee.Create(
            clientCtrlNbr: ControlNumber.Create(1),
            userId: userId,
            employeeNumber: employeeNumber,
            ssn: "123456789",
            gender: Gender.Male,
            race: Race.White,
            birthDate: new DateTime(1990, 1, 1),
            employmentDate: new DateTime(2020, 1, 1),
            employmentStatusCtrlNbr: ControlNumber.Create(1),
            email: "test@example.com",
            invitedByUserId: "inviter",
            invitedByUserName: "Inviter Name");

    private static DefaultHttpContext BuildHttpContext(params Claim[] claims)
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
        return ctx;
    }

    private sealed class FakeEmployeeRepository(Employee? employeeByUserId, Employee? employeeByNumber) : IEmployeeRepository
    {
        public Task<Employee?> GetByEmployeeNumberAsync(string employeeNumber) => Task.FromResult(employeeByNumber);
        public Task<Employee?> GetBySocialSecurityNumberAsync(string socialSecurityNumber, CancellationToken ct = default) => Task.FromResult<Employee?>(null);
        public Task<Employee?> GetByUserIdAsync(string userId, CancellationToken ct = default) => Task.FromResult(employeeByUserId);

        public Task<List<Employee>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr) => Task.FromResult(new List<Employee>());
        public Task<List<Employee>> GetListByClientCtrlNbrAsync(ControlNumber clientCtrlNbr) => Task.FromResult(new List<Employee>());
        public Task<List<Employee>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> ctrlNbrs, CancellationToken ct = default) => Task.FromResult(new List<Employee>());

        public Task<List<Employee>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<Employee>());
        public Task<List<Employee>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<Employee>());
        public Task<Employee?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<Employee?>(null);
        public Task<Employee?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<Employee?>(null);
        public Task AddAsync(Employee entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Employee entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public void Add(Employee entity) { }
        public void Update(Employee entity) { }
        public void Remove(Employee entity) { }
    }
}
