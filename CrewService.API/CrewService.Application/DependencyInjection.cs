using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
