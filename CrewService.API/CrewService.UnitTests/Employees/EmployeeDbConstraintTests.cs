using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;
using CrewService.Infrastructure.Models.UserAccount;
using CrewService.Persistance.Data;
using CrewService.UnitTests.Fixtures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CrewService.UnitTests.Employees;

public sealed class EmployeeDbConstraintTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CrewServiceDbContext _crewContext;
    private readonly UserAccessDbContext _userContext;

    public EmployeeDbConstraintTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var crewOptions = new DbContextOptionsBuilder<CrewServiceDbContext>()
            .UseSqlite(_connection)
            .Options;
        _crewContext = new CrewServiceDbContext(crewOptions, new TestCurrentUserService(), new TestFieldEncryptor());
        _crewContext.Database.EnsureCreated();

        var userOptions = new DbContextOptionsBuilder<UserAccessDbContext>()
            .UseSqlite(_connection)
            .Options;
        _userContext = new UserAccessDbContext(userOptions);
        _userContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task Employee_Insert_WithNullSocialSecurityNumber_ThrowsDbUpdateException()
    {
        var status = EmploymentStatus.Create(1, "ACT", "Active", 1, "A");
        _crewContext.Set<EmploymentStatus>().Add(status);
        await _crewContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var employee = Employee.Create(
            clientCtrlNbr: 1,
            userId: "user-1",
            employeeNumber: "E001",
            ssn: null!,
            gender: Gender.Male,
            race: Race.White,
            birthDate: new DateTime(1990, 1, 1),
            employmentDate: new DateTime(2020, 1, 1),
            employmentStatusCtrlNbr: status.CtrlNbr,
            email: "employee@example.com",
            invitedByUserId: "system",
            invitedByUserName: "System");

        _crewContext.Set<Employee>().Add(employee);

        await Assert.ThrowsAsync<DbUpdateException>(() => _crewContext.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Employee_Insert_WithDefaultBirthDate_ThrowsDbUpdateException()
    {
        var status = EmploymentStatus.Create(1, "ACT", "Active", 1, "A");
        _crewContext.Set<EmploymentStatus>().Add(status);
        await _crewContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var employee = Employee.Create(
            clientCtrlNbr: 1,
            userId: "user-2",
            employeeNumber: "E002",
            ssn: "123-45-6789",
            gender: Gender.Male,
            race: Race.White,
            birthDate: default,
            employmentDate: new DateTime(2020, 1, 1),
            employmentStatusCtrlNbr: status.CtrlNbr,
            email: "employee2@example.com",
            invitedByUserId: "system",
            invitedByUserName: "System");

        _crewContext.Set<Employee>().Add(employee);

        await Assert.ThrowsAsync<DbUpdateException>(() => _crewContext.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task User_Insert_WithEmployeeNumberAndMissingNames_ThrowsDbUpdateException()
    {
        var user = new User
        {
            UserName = "employee.user",
            Email = "employee.user@example.com",
            EmployeeNumber = "E003",
            FirstName = null,
            LastName = null
        };

        _userContext.Set<User>().Add(user);

        await Assert.ThrowsAsync<DbUpdateException>(() => _userContext.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    public void Dispose()
    {
        _crewContext.Dispose();
        _userContext.Dispose();
        _connection.Dispose();
    }
}
