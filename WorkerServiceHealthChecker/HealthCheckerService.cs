using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WorkerServiceHealthChecker;

public class HealthCheckerService : IHealthCheckerService
{
    private readonly HealthCheckService _healthCheckService;

    public HealthCheckerService(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }
    
    public Task<HealthReport> GetHealthReportAsync(CancellationToken ctx) =>  _healthCheckService.CheckHealthAsync(ctx);
}