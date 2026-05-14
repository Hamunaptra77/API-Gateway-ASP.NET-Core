using System.Text;
using ApiGateway.Models;
using Serilog;

namespace ApiGateway.Services;

/// <summary>
/// Reverse proxy service implementation
/// </summary>
public class ProxyService : IProxyService
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "connection",
        "keep-alive",
        "proxy-authenticate",
        "proxy-authorization",
        "te",
        "trailers",
        "transfer-encoding",
        "upgrade",
        "content-length"
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GatewayConfig _config;
    private readonly ILogger _logger = Log.ForContext<ProxyService>();

    public ProxyService(IHttpClientFactory httpClientFactory, GatewayConfig config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public Dictionary<string, string> GetUpstreamServices()
    {
        return new Dictionary<string, string>(_config.Upstreams);
    }

    public async Task<bool> IsServiceHealthyAsync(string serviceName)
    {
        if (!_config.Upstreams.TryGetValue(serviceName, out var upstreamUrl))
            return false;

        try
        {
            var client = _httpClientFactory.CreateClient();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await client.GetAsync($"{upstreamUrl}/health", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Health check failed for service {Service}", serviceName);
            return false;
        }
    }

    public async Task<IResult> ProxyRequestAsync(string serviceName, string path, HttpContext context)
    {
        // Validate service
        if (!_config.Upstreams.TryGetValue(serviceName, out var upstreamUrl))
        {
            return Results.NotFound(new { error = $"Service '{serviceName}' not found" });
        }

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Prepare upstream URL
            var targetUrl = $"{upstreamUrl}{path}";
            if (!string.IsNullOrEmpty(context.Request.QueryString.Value))
            {
                targetUrl += context.Request.QueryString.Value;
            }

            _logger.Information("Proxying {Method} {Path} to {Service} ({UpstreamUrl})",
                context.Request.Method, context.Request.Path, serviceName, targetUrl);

            // Create request
            var request = new HttpRequestMessage(
                new HttpMethod(context.Request.Method),
                targetUrl
            );

            // Copy headers
            CopyHeaders(context.Request, request);

            // Copy body if needed
            if (context.Request.ContentLength > 0)
            {
                request.Content = new StreamContent(context.Request.Body);
                if (context.Request.ContentType != null)
                {
                    request.Content.Headers.ContentType = 
                        System.Net.Http.Headers.MediaTypeHeaderValue.Parse(context.Request.ContentType);
                }
            }

            // Send request
            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            stopwatch.Stop();

            _logger.Information("Received {StatusCode} from {Service} in {ElapsedMs}ms",
                (int)response.StatusCode, serviceName, stopwatch.ElapsedMilliseconds);

            // Copy response headers
            CopyResponseHeaders(response, context.Response);

            // Set status code
            context.Response.StatusCode = (int)response.StatusCode;

            // Copy response body
            await response.Content.CopyToAsync(context.Response.Body);

            return Results.Empty;
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "HTTP request failed to service {Service}", serviceName);
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }
        catch (TaskCanceledException ex)
        {
            _logger.Error(ex, "Request timeout for service {Service}", serviceName);
            return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error proxying to service {Service}", serviceName);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private void CopyHeaders(HttpRequest sourceRequest, HttpRequestMessage targetRequest)
    {
        foreach (var header in sourceRequest.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
                continue;

            try
            {
                if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(targetRequest.RequestUri?.ToString() ?? "");
                    targetRequest.Headers.Host = uri.Host;
                }
                else if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    // Content-Type is handled separately
                    continue;
                }
                else
                {
                    targetRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to copy header {Header}", header.Key);
            }
        }
    }

    private void CopyResponseHeaders(HttpResponseMessage sourceResponse, HttpResponse targetResponse)
    {
        foreach (var header in sourceResponse.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
                continue;

            try
            {
                targetResponse.Headers.TryAdd(header.Key, header.Value.ToArray());
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to copy response header {Header}", header.Key);
            }
        }

        foreach (var header in sourceResponse.Content.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
                continue;

            try
            {
                targetResponse.Headers.TryAdd(header.Key, header.Value.ToArray());
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to copy content header {Header}", header.Key);
            }
        }
    }
}
