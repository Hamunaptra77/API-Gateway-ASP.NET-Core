using Serilog;

namespace ApiGateway.Middleware;

/// <summary>
/// Middleware for managing request headers
/// </summary>
public class RequestHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger = Log.ForContext<RequestHeaderMiddleware>();

    public RequestHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        var response = context.Response;

        // HSTS Header
        response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";

        // X-Frame-Options
        response.Headers["X-Frame-Options"] = "DENY";

        // X-Content-Type-Options
        response.Headers["X-Content-Type-Options"] = "nosniff";

        // X-XSS-Protection
        response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Remove Server header
        response.Headers.Remove("Server");
        response.Headers.Remove("X-Powered-By");

        await _next(context);
    }
}
