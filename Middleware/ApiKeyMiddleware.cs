using Serilog;

namespace ApiGateway.Middleware;

/// <summary>
/// Middleware for API Key authentication
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GatewayConfig _config;
    private readonly ILogger _logger = Log.ForContext<ApiKeyMiddleware>();

    public ApiKeyMiddleware(RequestDelegate next, GatewayConfig config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.ToString();

        // Check if path requires API key
        if (IsProtectedPath(path) && !IsExemptPath(path))
        {
            var apiKey = ExtractApiKey(context.Request);

            if (string.IsNullOrEmpty(apiKey) || apiKey != _config.ApiKey)
            {
                _logger.Warning("Unauthorized access attempt to {Path} from {RemoteIP}",
                    path, context.Connection.RemoteIpAddress);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing API key" });
                return;
            }

            _logger.Information("Authorized request to {Path}", path);
        }

        await _next(context);
    }

    private string? ExtractApiKey(HttpRequest request)
    {
        // Check Authorization header (Bearer token)
        var auth = request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return auth.Substring("Bearer ".Length).Trim();
        }

        // Check X-API-Key header
        if (request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            return apiKeyHeader.ToString();
        }

        // Check query parameter
        if (request.Query.TryGetValue("api_key", out var apiKeyQuery))
        {
            return apiKeyQuery.ToString();
        }

        return null;
    }

    private bool IsProtectedPath(string path)
    {
        return _config.ProtectedPrefixes.Any(prefix => path.StartsWith(prefix));
    }

    private bool IsExemptPath(string path)
    {
        return _config.ExemptPaths.Any(exempt => 
            path.StartsWith(exempt) || path.Equals(exempt, StringComparison.OrdinalIgnoreCase));
    }
}
