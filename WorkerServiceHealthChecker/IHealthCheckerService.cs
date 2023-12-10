using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WorkerServiceHealthChecker;

public interface IHealthCheckerService
{
    Task<HealthReport> GetHealthReportAsync(CancellationToken ctx);
}