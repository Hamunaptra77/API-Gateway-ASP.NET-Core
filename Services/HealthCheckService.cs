using ApiGateway.Models;
using Serilog;

namespace ApiGateway.Services;

/// <summary>
/// Health check service implementation
/// </summary>
public class HealthCheckService : IHealthCheckService
{
    private readonly IProxyService _proxyService;
    private readonly GatewayConfig _config;
    private readonly ILogger _logger = Log.ForContext<HealthCheckService>();
    private Timer? _healthCheckTimer;
    private readonly DateTime _startTime = DateTime.UtcNow;

    public HealthCheckService(IProxyService proxyService, GatewayConfig config)
    {
        _proxyService = proxyService;
        _config = config;
    }

    public async Task<HealthStatus> CheckHealthAsync()
    {
        var status = new HealthStatus
        {
            UptimeSeconds = (int)(DateTime.UtcNow - _startTime).TotalSeconds
        };

        var upstreams = _proxyService.GetUpstreamServices();
        var healthCheckTasks = upstreams.Select(async kvp =>
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var isHealthy = await _proxyService.IsServiceHealthyAsync(kvp.Key);
            stopwatch.Stop();

            return new ServiceHealth
            {
                Name = kvp.Key,
                Url = kvp.Value,
                IsHealthy = isHealthy,
                Status = isHealthy ? "Healthy" : "Unhealthy",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        });

        var results = await Task.WhenAll(healthCheckTasks);
        
        foreach (var serviceHealth in results)
        {
            status.Services[serviceHealth.Name] = serviceHealth;
        }

        // Determine overall status
        var allHealthy = results.All(s => s.IsHealthy);
        status.Status = allHealthy ? "Healthy" : "Degraded";

        _logger.Information("Health check completed. Status: {Status}, Services: {ServiceCount}",
            status.Status, status.Services.Count);

        return status;
    }

    public void StartPeriodicHealthChecks(TimeSpan interval)
    {
        _healthCheckTimer = new Timer(async _ =>
        {
            try
            {
                await CheckHealthAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Periodic health check failed");
            }
        }, null, interval, interval);

        _logger.Information("Periodic health checks started with interval {IntervalSeconds}s", interval.TotalSeconds);
    }

    public void StopPeriodicHealthChecks()
    {
        _healthCheckTimer?.Dispose();
        _logger.Information("Periodic health checks stopped");
    }
}
