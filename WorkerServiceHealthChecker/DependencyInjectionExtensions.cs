using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WorkerServiceHealthChecker;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddWorkerServiceHealthChecker(this IServiceCollection services, Action<HealthCheckParameters>? configuration)
    {
        var serviceConfig = new HealthCheckParameters();
        configuration?.Invoke(serviceConfig);
        services.AddSingleton(serviceConfig);
        
        services.TryAddSingleton<IHealthCheckerService, HealthCheckerService>();
        
        if (serviceConfig.Tcp)
        {
            services.AddHostedService<TcpHealthCheckServer>();
        }
        else
        {
            services.AddHostedService<HttpHealthCheckServer>();
        }
        
        return services;
    }
}