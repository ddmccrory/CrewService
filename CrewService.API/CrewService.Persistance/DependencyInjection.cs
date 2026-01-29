using CrewService.Domain.Repositories;
using CrewService.Infrastructure.Models.UserAccount;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Persistance;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistance(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("SQLiteConnection");

        //services.AddDbContext<UserAccessDbContext>(options => options
        //        .UseSqlServer(connectionString, o => o.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "identity")));

        //services.AddDbContext<CrewAssignmentDbContext>(options => options
        //        .UseSqlServer(connectionString, o => o.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "crew_assignment")));

        services.AddDbContext<UserAccessDbContext>(options => options.UseSqlite(connectionString));

        services.AddDbContext<CrewAssignmentDbContext>(options => options.UseSqlite(connectionString));

        services.AddIdentityCore<User>(options =>
        {
            options.Password.RequiredLength = 8;
            options.SignIn.RequireConfirmedEmail = true;
        }).AddRoles<IdentityRole>()
          .AddEntityFrameworkStores<UserAccessDbContext>();

        services.AddScoped<IParentRepository, ParentRepository>();

        services.AddScoped<IRailroadRepository, RailroadRepository>();

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();

        return services;
    }
}
