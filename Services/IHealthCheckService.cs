using ApiGateway.Models;

namespace ApiGateway.Services;

/// <summary>
/// Interface for health check service
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Check health of all services
    /// </summary>
    Task<HealthStatus> CheckHealthAsync();

    /// <summary>
    /// Start periodic health checks
    /// </summary>
    void StartPeriodicHealthChecks(TimeSpan interval);

    /// <summary>
    /// Stop periodic health checks
    /// </summary>
    void StopPeriodicHealthChecks();
}
