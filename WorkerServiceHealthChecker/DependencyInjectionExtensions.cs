using Microsoft.Extensions.DependencyInjection;

namespace WorkerServiceHealthChecker;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddWorkerServiceHealthChecks(this IServiceCollection services, Action<HealthCheckParameters>? configuration)
    {
        var serviceConfig = new HealthCheckParameters();
        configuration?.Invoke(serviceConfig);
        services.AddSingleton(serviceConfig);
        services.AddHostedService<HealthCheckServer>();
        
        return services;
    }
}