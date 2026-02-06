using CrewService.Domain.Interfaces;
using CrewService.Infrastructure.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure outbox publisher options
        services.Configure<OutboxPublisherOptions>(
            configuration.GetSection(OutboxPublisherOptions.SectionName));

        // Register message publisher (use NoOp for dev, replace with real impl for prod)
        services.AddSingleton<IMessagePublisher, NoOpMessagePublisher>();

        // Register in-process dispatcher (singleton - shared channel)
        services.AddSingleton<OutboxDispatcher>();
        services.AddSingleton<IOutboxDispatcher>(sp => sp.GetRequiredService<OutboxDispatcher>());

        // Register background outbox publisher service (hybrid mode)
        services.AddHostedService<OutboxPublisherService>();

        return services;
    }
}
