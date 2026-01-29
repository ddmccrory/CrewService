using CrewService.Infrastructure.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddProblemDetails()
                .AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
