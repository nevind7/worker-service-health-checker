namespace WorkerServiceHealthChecker;

public sealed class HealthCheckParameters
{
    public int Port { get; set; }
    public string Endpoint { get; set; } = "health";
    public bool UseHttps { get; set; } = false;
    public bool Silent { get; set; }
}