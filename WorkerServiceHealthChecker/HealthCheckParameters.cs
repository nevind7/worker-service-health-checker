namespace WorkerServiceHealthChecker;

public sealed class HealthCheckParameters
{
    public int Port { get; set; } = 5000;
    public string Endpoint { get; set; } = "healthz";
    public bool UseHttps { get; set; } = false;
    public bool Tcp { get; set; } = false;
    public bool SilentMode { get; set; } = false;
}