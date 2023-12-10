using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using WorkerServiceHealthChecker.Exceptions;

namespace WorkerServiceHealthChecker;
public class HttpHealthCheckServer : BackgroundService
{
    private readonly HttpListener _httpListener = new();
    private readonly bool _silentMode;
    private readonly IHealthCheckerService _healthCheckerService;

    public Func<HttpListenerRequest, bool>? RequestPredicate { get; set; }

    public HttpHealthCheckServer(IHealthCheckerService healthCheckerService, HealthCheckParameters healthCheckParameters)
    {
        _healthCheckerService = healthCheckerService;
        var s = healthCheckParameters.UseHttps ? "s" : "";
        _httpListener.Prefixes.Add($"http{s}://+:{healthCheckParameters.Port}/{healthCheckParameters.Endpoint}/");

        _silentMode = healthCheckParameters.SilentMode;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _httpListener.Start();
        }
        catch (HttpListenerException ex)
        {
            var errorMessage = "You may need to grant permissions to your user account if not running as Administrator:" +
                                        $" \"netsh http add urlacl url={ _httpListener.Prefixes.First()} user=DOMAIN\\\\user\"";
            
            Trace.WriteLine($"Error starting {nameof(HttpHealthCheckServer)}. " + errorMessage);
            
            if (!_silentMode)
            {
                throw new ListenerException(errorMessage, ex);
            }
            
            return Task.CompletedTask;
        }
        
        return Task.Factory.StartNew(delegate
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var getContext = _httpListener.GetContextAsync();
                    getContext.Wait(stoppingToken);
                    var context = getContext.Result;
                    
                    _ = Task.Factory.StartNew(async delegate
                    {
                        await HandleRequest(context, stoppingToken);
                    }, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error in {nameof(HttpHealthCheckServer)}: {ex}");
            }
            finally
            {
                _httpListener.Stop();
                _httpListener.Close();
            }
        }, TaskCreationOptions.LongRunning);
    }
    
    private async Task HandleRequest(HttpListenerContext context, CancellationToken ctx)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var predicate = RequestPredicate;

            if (predicate != null && !predicate(request))
            {
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            try
            {
                await HandleHealthCheckRequest(response, ctx);
            }
            catch (Exception ex)
            {
                response.StatusCode = 503;

                if (!string.IsNullOrWhiteSpace(ex.Message))
                {
                    await using var writer = new StreamWriter(response.OutputStream);
                    await writer.WriteAsync(ex.Message);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (!_httpListener.IsListening)
                return;

            Trace.WriteLine($"Error in {nameof(HttpHealthCheckServer)}: {ex}");

            try
            {
                response.StatusCode = 500;
            }
            catch
            {
                // Might be too late in request processing to set response code, so just ignore.
            }
        }
        finally
        {
            response.Close();
        }
    }
    
    private async Task HandleHealthCheckRequest(HttpListenerResponse response, CancellationToken ctx)
    {
        var healthCheckReport = await _healthCheckerService.GetHealthReportAsync(ctx);

        var healthCheckStatus = healthCheckReport.Status == HealthStatus.Healthy
            ? "All services are healthy."
            : "Unhealthy: " + string.Join(", ",
                healthCheckReport.Entries.Select(x => x.Key + ": " + x.Value.Status));
        
        await using (var writer = new StreamWriter(response.OutputStream))
            await writer.WriteAsync(healthCheckStatus);
        await response.OutputStream.DisposeAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_httpListener.IsListening) _httpListener.Stop();
        await base.StopAsync(cancellationToken);
    }
}