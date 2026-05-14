# Build & Development Guide

## Anforderungen

- .NET 8.0 SDK oder höher
- PowerShell oder Bash
- Docker (optional)

## Installation

### 1. .NET SDK prüfen

```bash
dotnet --version
# Output: 8.0.x
```

Wenn nicht installiert: [dotnet.microsoft.com](https://dotnet.microsoft.com)

### 2. Projekt initialisieren

```bash
cd api-gateway-aspnet

# NuGet-Pakete wiederherstellen
dotnet restore
```

## Development

### Lokal starten (Development)

```bash
dotnet run
```

- Gateway: `http://localhost:5000`
- Dashboard: `http://localhost:5000/dashboard`
- API Info: `http://localhost:5000/api-info`
- Health: `http://localhost:5000/health`

### Logs anschauen

```bash
# Linux/Mac
tail -f logs/gateway-*.log

# Windows PowerShell
Get-Content logs/gateway-*.log -Wait
```

### Hot Reload (Development)

```bash
dotnet watch run
```

## Build

### Development Build

```bash
dotnet build
```

Artifacts: `bin/Debug/net8.0/`

### Release Build

```bash
dotnet build -c Release
```

Artifacts: `bin/Release/net8.0/`

### Publish (Standalone)

```bash
dotnet publish -c Release -o ./publish
```

Für Windows-Distribution:
```bash
dotnet publish -c Release -r win-x64 -o ./publish
```

## Docker

### Build

```bash
docker build -t api-gateway:latest .

# Mit Progress-Output
docker build --progress=plain -t api-gateway:latest .
```

### Run

```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Gateway__ApiKey="your-api-key" \
  api-gateway:latest
```

### Debug Container

```bash
docker run -it --rm \
  -p 8080:8080 \
  --name api-gateway-debug \
  api-gateway:latest
```

## Konfiguration

### appsettings.json (Development)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Gateway": {
    "ApiKey": "X1-8CAl9ZOTZjHzNDg-OXGfjwZCjGWrxjMumSI3dcPZ2lbIZUPpdB2zNjjtramKW",
    "Domain": "api.example.com",
    "EnableHttpsRedirect": true,
    "RequestTimeoutSeconds": 30,
    "MaxRequestSizeMB": 50
  }
}
```

### Umgebungsvariablen

```bash
# Linux/Mac
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://+:8080
export Gateway__ApiKey="your-api-key"

# Windows PowerShell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "http://+:8080"
$env:Gateway__ApiKey = "your-api-key"

# Dann starten:
dotnet run
```

## Testing

### Einfacher Request

```bash
# Health Check
curl http://localhost:5000/health

# API Info
curl http://localhost:5000/api-info

# Mit API Key (Terminal API)
curl -H "Authorization: Bearer X1-8CAl9ZOTZjHzNDg-OXGfjwZCjGWrxjMumSI3dcPZ2lbIZUPpdB2zNjjtramKW" \
  http://localhost:5000/api/terminals/sessions

# Mit X-API-Key Header
curl -H "X-API-Key: X1-8CAl9ZOTZjHzNDg-OXGfjwZCjGWrxjMumSI3dcPZ2lbIZUPpdB2zNjjtramKW" \
  http://localhost:5000/api/memory/search
```

### Load Test (mit Apache Bench)

```bash
# 1000 requests, 10 concurrent
ab -n 1000 -c 10 http://localhost:5000/health

# Mit API Key
ab -n 1000 -c 10 \
  -H "X-API-Key: X1-8CAl9ZOTZjHzNDg-OXGfjwZCjGWrxjMumSI3dcPZ2lbIZUPpdB2zNjjtramKW" \
  http://localhost:5000/api-info
```

## Debugging

### Visual Studio Code

1. Installiere "C# Dev Kit" Extension
2. Starte: `dotnet run` oder use breakpoints
3. Output: `Ctrl+Shift+M`

### Visual Studio

1. Öffne `.csproj` in Visual Studio
2. F5 zum Starten mit Debugger
3. Breakpoints setzen
4. Watch/Locals

### Command Line Debugging

```bash
# Mit Enhanced Logging
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Logs gucken
tail -f logs/gateway-*.log
```

## Deployment

### Linux Server

```bash
# Publish
dotnet publish -c Release -o /app/publish

# Systemd Service erstellen (/etc/systemd/system/api-gateway.service)
[Unit]
Description=API Gateway
After=network.target

[Service]
Type=notify
User=www-data
WorkingDirectory=/app/publish
ExecStart=/usr/bin/dotnet /app/publish/ApiGateway.dll
Environment="ASPNETCORE_ENVIRONMENT=Production"
Restart=on-failure
RestartSec=10

[Install]
WantedBy=multi-user.target

# Aktivieren
sudo systemctl daemon-reload
sudo systemctl enable api-gateway
sudo systemctl start api-gateway
```

### Docker Compose

```yaml
api-gateway:
  build:
    context: ./api-gateway-aspnet
    dockerfile: Dockerfile
  ports:
    - "8080:8080"
  environment:
    ASPNETCORE_ENVIRONMENT: Production
    Gateway__ApiKey: "X1-8CAl9ZOTZjHzNDg-OXGfjwZCjGWrxjMumSI3dcPZ2lbIZUPpdB2zNjjtramKW"
    Gateway__Domain: "api.example.com"
  depends_on:
    - open-terminal-api
    - memory-api
  volumes:
    - ./api-gateway-aspnet/logs:/app/logs
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
    interval: 30s
    timeout: 3s
    retries: 3
```

## Performance Tuning 🚀

### Kestrel Server-Optionen

```csharp
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50 MB
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(30);
});
```

### Connection Pooling

```csharp
builder.Services.AddHttpClient()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });
```

### GC Optimization

```bash
# Umgebungsvariablen
export DOTNET_GCRetainVM=1
export DOTNET_GCServer=1
export DOTNET_GCConcurrent=1
```

## Troubleshooting 🔧

### Port-Konflikt

```bash
# Port 5000 suchen
lsof -i :5000

# Oder mit netstat
netstat -tulpn | grep 5000

# Beenden
kill -9 <PID>
```

### Certificate Issues

```bash
# Zertifikate generieren (Development)
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Memory Leak Debugging

```bash
# Heap Dump erstellen
dotnet dump collect -p <PID>

# Mit dotnet-symbol analysieren
dotnet symbol <dump-file>
```

---

**Last Updated**: 2024-05-14
