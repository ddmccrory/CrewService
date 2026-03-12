using CrewService.Application.HolidayManagement;
using CrewService.Application.Payroll;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Holiday services
        services.AddScoped<HolidayAutoGenerationService>();
        services.AddScoped<HolidayQualificationService>();
        services.AddScoped<HolidayPayrollGenerationService>();

        return services;
    }
}
