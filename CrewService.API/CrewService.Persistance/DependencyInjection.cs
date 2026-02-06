using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Infrastructure.Models.UserAccount;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using CrewService.Persistance.Services;
using CrewService.Persistance.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Persistance;

public static class DependencyInjection
{

    public static IServiceCollection AddPersistance(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        string? connectionString = configuration.GetConnectionString("SQLiteConnection");

        //services.AddDbContext<UserAccessDbContext>(options => options
        //        .UseSqlServer(connectionString, o => o.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "identity")));

        //services.AddDbContext<CrewAssignmentDbContext>(options => options
        //        .UseSqlServer(connectionString, o => o.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "crew_assignment")));

        services.AddDbContext<UserAccessDbContext>(options => options.UseSqlite(connectionString));

        services.AddDbContext<CrewServiceDbContext>(options => options.UseSqlite(connectionString));

        services.AddIdentityCore<User>(options =>
        {
            options.Password.RequiredLength = 8;
            options.SignIn.RequireConfirmedEmail = true;
        }).AddRoles<IdentityRole>()
          .AddEntityFrameworkStores<UserAccessDbContext>();

        // Orchestration UoW Factory (transient - creates new UoW per request)
        services.AddTransient<IOrchestrationUnitOfWorkFactory, OrchestrationUnitOfWorkFactory>();

        // Core Repositories
        services.AddScoped<IParentRepository, ParentRepository>();
        services.AddScoped<IRailroadRepository, RailroadRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IRailroadEmployeeRepository, RailroadEmployeeRepository>();
        services.AddScoped<IRailroadPoolRepository, RailroadPoolRepository>();
        services.AddScoped<IRailroadPoolEmployeeRepository, RailroadPoolEmployeeRepository>();

        // ContactType Repositories
        services.AddScoped<IAddressTypeRepository, AddressTypeRepository>();
        services.AddScoped<IPhoneNumberTypeRepository, PhoneNumberTypeRepository>();
        services.AddScoped<IEmailAddressTypeRepository, EmailAddressTypeRepository>();

        // Employment Repositories
        services.AddScoped<IEmploymentStatusRepository, EmploymentStatusRepository>();
        services.AddScoped<IEmploymentStatusHistoryRepository, EmploymentStatusHistoryRepository>();
        services.AddScoped<IEmployeePriorServiceCreditRepository, EmployeePriorServiceCreditRepository>();

        // Seniority Repositories
        services.AddScoped<ICraftRepository, CraftRepository>();
        services.AddScoped<IRosterRepository, RosterRepository>();
        services.AddScoped<ISeniorityRepository, SeniorityRepository>();
        services.AddScoped<ISeniorityStateRepository, SeniorityStateRepository>();

        return services;
    }
}
