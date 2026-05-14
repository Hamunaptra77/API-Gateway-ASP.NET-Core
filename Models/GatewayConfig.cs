namespace ApiGateway.Models;

/// <summary>
/// Configuration for the API Gateway
/// </summary>
public class GatewayConfig
{
    /// <summary>
    /// API Key for authentication
    /// </summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Domain name for HTTPS redirect
    /// </summary>
    public string Domain { get; set; } = "";

    /// <summary>
    /// Upstream services mapping
    /// </summary>
    public Dictionary<string, string> Upstreams { get; set; } = new()
    {
        ["terminals"] = "http://open-terminal-api:8000",
        ["memory"] = "http://memory-api:8001",
        ["vector"] = "http://vector-memory-api:8002",
        ["filesystem"] = "http://filesystem-api:8003",
        ["summarizer"] = "http://summarizer-api:8004",
    };

    /// <summary>
    /// Paths that require API key authentication
    /// </summary>
    public List<string> ProtectedPrefixes { get; set; } = new()
    {
        "/api/",
        "/openapi.json",
        "/docs",
        "/redoc"
    };

    /// <summary>
    /// Paths exempt from authentication
    /// </summary>
    public List<string> ExemptPaths { get; set; } = new()
    {
        "/health",
        "/.well-known/acme-challenge"
    };

    /// <summary>
    /// Enable HTTPS redirect
    /// </summary>
    public bool EnableHttpsRedirect { get; set; } = true;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum request body size in MB
    /// </summary>
    public int MaxRequestSizeMB { get; set; } = 50;
}

/// <summary>
/// API Response wrapper
/// </summary>
public class ApiResponse<T>
{
    public T? Data { get; set; }
    public string? Error { get; set; }
    public bool Success => Error == null;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Health status information
/// </summary>
public class HealthStatus
{
    public string Status { get; set; } = "Healthy";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, ServiceHealth> Services { get; set; } = new();
    public int UptimeSeconds { get; set; }
}

/// <summary>
/// Individual service health status
/// </summary>
public class ServiceHealth
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = "Unknown";
    public int? StatusCode { get; set; }
    public string? Error { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public int ResponseTimeMs { get; set; }
}

/// <summary>
/// Proxy request information
/// </summary>
public class ProxyRequestInfo
{
    public string ServiceName { get; set; } = "";
    public string UpstreamUrl { get; set; } = "";
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public DateTime RequestTime { get; set; } = DateTime.UtcNow;
    public int? ResponseStatusCode { get; set; }
    public int ResponseTimeMs { get; set; }
}
