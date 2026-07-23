using CrewService.Application.AbsenceVacancy;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using CrewService.Persistance.UnitOfWork;
using CrewService.UnitTests.Fixtures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrewService.UnitTests.Persistence;

public class OrchestrationUnitOfWorkNestedTransactionTests
{
    [Fact]
    public async Task DbAbsenceApprovalPolicyResolver_CanRunInsideActiveOrchestrationUow_WithoutNestedTransactionFailure()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var currentUser = new TestCurrentUserService();
        var encryptor = new TestFieldEncryptor();

        var crewOptions = new DbContextOptionsBuilder<CrewServiceDbContext>()
            .UseSqlite(connection)
            .Options;

        var userOptions = new DbContextOptionsBuilder<UserAccessDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var crewContext = new CrewServiceDbContext(crewOptions, currentUser, encryptor);
        await using var userContext = new UserAccessDbContext(userOptions);

        await crewContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await userContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var uowFactory = new OrchestrationUnitOfWorkFactory(
            connection,
            crewContext,
            userContext,
            currentUser,
            NullLoggerFactory.Instance);

        await using var outerUow = await uowFactory.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);

        var repository = new AbsenceApprovalPolicyRepository(crewContext, currentUser);
        var resolver = new DbAbsenceApprovalPolicyResolver(repository);

        var absenceCode = AbsenceCode.Create(
            railroadCtrlNbr: 1,
            code: "VAC",
            description: "Vacation",
            isExcused: true,
            isCompensated: true,
            requiresApproval: true,
            isSystemOnly: false,
            isHolidayExempt: false,
            defaultAutoMarkUpHours: null,
            isActive: true);

        var ex = await Record.ExceptionAsync(() => resolver.ResolveAsync(absenceCode, TestContext.Current.CancellationToken));

        Assert.Null(ex);

        await outerUow.RollbackAsync(TestContext.Current.CancellationToken);
    }
}
