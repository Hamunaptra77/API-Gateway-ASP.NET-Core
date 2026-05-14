using ApiGateway.Middleware;
using ApiGateway.Services;

namespace ApiGateway.Extensions;

/// <summary>
/// Extension methods for middleware registration
/// </summary>
public static class MiddlewareExtensions
{
    public static void UseGatewayMiddleware(this WebApplication app, GatewayConfig config)
    {
        app.UseMiddleware<HttpsRedirectMiddleware>();
        app.UseMiddleware<ApiKeyMiddleware>();
        app.UseMiddleware<RequestHeaderMiddleware>();
    }
}
