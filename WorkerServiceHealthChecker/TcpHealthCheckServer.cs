using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using WorkerServiceHealthChecker.Exceptions;

namespace WorkerServiceHealthChecker;

public class TcpHealthCheckServer : BackgroundService
{
    private readonly bool _silentMode;
    private readonly IHealthCheckerService _healthCheckerService;
    private readonly WorkerServiceTcpListener _tcpListener;
    private HealthStatus _healthStatus;

    public TcpHealthCheckServer(IHealthCheckerService healthCheckerService,  HealthCheckParameters healthCheckParameters)
    {
        _silentMode = healthCheckParameters.SilentMode;
        _healthCheckerService = healthCheckerService;
        _tcpListener = new WorkerServiceTcpListener(IPAddress.Any, healthCheckParameters.Port);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() => _tcpListener.Stop());
        
        try
        {
            await HealthCheckAndConnect(stoppingToken);
        }
        catch (Exception)
        {
            // ignored
        }
    }
    
    private async Task HealthCheckAndConnect(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && !_tcpListener.IsListening)
        {
            _healthStatus = await GetHealthStatusAsync(stoppingToken);

            await SetTcpConnection(stoppingToken);
            
            await Task.Delay(3000, stoppingToken);
        }
    }
    
    private async Task<HealthStatus> GetHealthStatusAsync(CancellationToken ctx)
    {
        try
        {
            var healthCheckReport = await _healthCheckerService.GetHealthReportAsync(ctx).ConfigureAwait(false);
            return healthCheckReport.Status;
        }
        catch (TaskCanceledException)
        {
            // operation was cancelled
        }

        return HealthStatus.Degraded;
    }
    
    private Task SetTcpConnection(CancellationToken stoppingToken)
    {
        if (_healthStatus == HealthStatus.Healthy)
        {
            StartTcpListener();
            return ListenForConnections(stoppingToken);
        }
        else
        {
            _tcpListener.Stop();
        }

        return Task.CompletedTask;
    }
    
    private async Task ListenForConnections(CancellationToken ctx)
    {
        IAsyncResult? result = default;
        
        if (_tcpListener.IsListening && _healthStatus == HealthStatus.Healthy)
        {
            result = _tcpListener.BeginAcceptSocket(async _ =>
            {
                _healthStatus = await GetHealthStatusAsync(ctx);
                
                await ListenForConnections(ctx);
            }, HandleHealthCheckResult(ctx));
        }
        else
        {
            _tcpListener.Stop(); 
            await HealthCheckAndConnect(ctx);
        }
        
        if (_tcpListener.IsListening && result != null)
        {
            _tcpListener.EndAcceptSocket(result);
        }
    }
    
    private object HandleHealthCheckResult(CancellationToken stoppingToken)
    {
        if (_healthStatus != HealthStatus.Healthy)
        {
            _tcpListener.Stop();
            _healthStatus = GetHealthStatusAsync(stoppingToken).GetAwaiter().GetResult();
        }
        else
        {
            StartTcpListener();
        }

        return _healthStatus;
    }
    
    private void StartTcpListener()
    {
        try
        {
            _tcpListener.Start();
        }
        catch (SocketException ex)
        {
            const string errorMessage = $"Error starting {nameof(TcpHealthCheckServer)}";
            
            Trace.WriteLine(errorMessage);
            
            if (!_silentMode)
            {
                throw new ListenerException(errorMessage, ex);
            }
        }
    }
    
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        if (_tcpListener.IsListening) _tcpListener.Stop();
        _tcpListener.Dispose();

        return base.StopAsync(cancellationToken);
    }
}