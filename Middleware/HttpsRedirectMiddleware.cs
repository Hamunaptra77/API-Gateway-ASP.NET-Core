using Serilog;

namespace ApiGateway.Middleware;

/// <summary>
/// Middleware for HTTPS redirect
/// </summary>
public class HttpsRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GatewayConfig _config;
    private readonly ILogger _logger = Log.ForContext<HttpsRedirectMiddleware>();

    public HttpsRedirectMiddleware(RequestDelegate next, GatewayConfig config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.ToString();

        // Skip HTTPS redirect for exempt paths
        if (_config.ExemptPaths.Any(exempt => 
            path.StartsWith(exempt) || path.Equals(exempt, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Redirect HTTP to HTTPS
        if (_config.EnableHttpsRedirect && 
            !context.Request.IsHttps && 
            context.Request.Scheme == "http")
        {
            var httpsUrl = $"https://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
            
            _logger.Information("Redirecting HTTP to HTTPS: {Path}", path);
            
            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location = httpsUrl;
            return;
        }

        await _next(context);
    }
}
