using System.Collections.Concurrent;
using ApiGateway.Middleware;
using ApiGateway.Models;
using ApiGateway.Services;
using Serilog;

var builder = WebApplicationBuilder.CreateBuilder(args);

// ===== Configuration =====
var gatewayConfig = new GatewayConfig();
builder.Configuration.Bind("Gateway", gatewayConfig);

// ===== Logging =====
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/gateway-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ===== Services =====
builder.Services.AddSingleton(gatewayConfig);
builder.Services.AddSingleton<IProxyService, ProxyService>();
builder.Services.AddSingleton<IHealthCheckService, HealthCheckService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Length", "Content-Range");
    });
});

builder.Services.AddHttpClient()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

// ===== App Build =====
var app = builder.Build();

// ===== Static Files =====
app.UseStaticFiles();

// ===== HTTPS Redirect =====
app.UseMiddleware<HttpsRedirectMiddleware>();

// ===== CORS =====
app.UseCors();

// ===== Request Logging =====
app.UseSerilogRequestLogging();

// ===== Authentication =====
app.UseMiddleware<ApiKeyMiddleware>();

// ===== Request Headers =====
app.UseMiddleware<RequestHeaderMiddleware>();

// ===== Health Check Endpoint =====
app.MapGet("/health", async (IHealthCheckService healthCheck) =>
{
    var status = await healthCheck.CheckHealthAsync();
    return Results.Ok(status);
}).WithName("Health").WithOpenApi();

// ===== API Gateway Root =====
app.MapGet("/api-info", (IProxyService proxy) =>
{
    var upstreams = proxy.GetUpstreamServices();
    return Results.Ok(new
    {
        gateway = "API Gateway v1.0 (ASP.NET Core)",
        services = upstreams.Keys.ToList(),
        timestamp = DateTime.UtcNow
    });
}).WithName("ApiInfo").WithOpenApi();

// ===== Dashboard =====
app.MapFallbackToFile("dashboard/index.html", "text/html").WithName("Dashboard");

// ===== Main Proxy Route =====
app.MapFallback(async (HttpContext context, IProxyService proxyService) =>
{
    try
    {
        var path = context.Request.Path.ToString();
        
        // Determine upstream service
        var (serviceName, relativePath) = ExtractServiceAndPath(path);
        
        if (string.IsNullOrEmpty(serviceName))
        {
            return Results.NotFound(new { error = "Service not found" });
        }

        // Proxy request
        var response = await proxyService.ProxyRequestAsync(
            serviceName, 
            relativePath, 
            context
        );

        return response;
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Proxy request failed");
        return Results.StatusCode(StatusCodes.Status502BadGateway);
    }
});

app.Run();

// ===== Helper Functions =====
static (string serviceName, string path) ExtractServiceAndPath(string requestPath)
{
    var parts = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
    
    if (parts.Length == 0 || parts[0] != "api")
        return (null!, "/");

    if (parts.Length < 2)
        return (null!, "/");

    var serviceName = parts[1];
    var relativePath = "/" + string.Join("/", parts.Skip(2));
    
    return (serviceName, relativePath);
}
