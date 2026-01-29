using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddHttpContextAccessor();

        return services;
    }
}
