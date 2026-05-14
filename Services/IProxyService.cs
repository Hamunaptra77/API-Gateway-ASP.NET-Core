namespace ApiGateway.Services;

/// <summary>
/// Interface for reverse proxy service
/// </summary>
public interface IProxyService
{
    /// <summary>
    /// Proxy a request to an upstream service
    /// </summary>
    Task<IResult> ProxyRequestAsync(string serviceName, string path, HttpContext context);

    /// <summary>
    /// Get all configured upstream services
    /// </summary>
    Dictionary<string, string> GetUpstreamServices();

    /// <summary>
    /// Check if a service is available
    /// </summary>
    Task<bool> IsServiceHealthyAsync(string serviceName);
}
