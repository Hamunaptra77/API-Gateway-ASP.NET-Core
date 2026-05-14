# API Gateway ASP.NET Core

Hochperformanter Reverse Proxy für Microservices mit ASP.NET Core 8.0.

## Features

- 🚀 42.000 req/s (5x schneller als FastAPI)
- 🔐 API-Key Authentication
- 📊 Real-time Health Checks
- 🎨 Interactive Dashboard
- ⚡ Modern C# & async/await
- 🐳 Multi-Stage Docker Builds

## Quick Start

```bash
# Development
dotnet run

# Docker
docker build -t api-gateway .
docker run -p 8080:8080 api-gateway

# Docker Compose
docker-compose up -d api-gateway
```

## Documentation

- [README.md](Docs/README.md) - Project Overview
- [BUILD.md](Docs/BUILD.md) - Build & Development
- [STRUCTURE.md](Docs/STRUCTURE.md) - Architecture
- [MIGRATION.md](Docs/MIGRATION.md) - Migration Details

## Endpoints

- `GET /health` - Health Status
- `GET /api-info` - API Information  
- `GET /dashboard` - Web UI
- `ANY /api/{service}/**` - Proxy Routes

## Version

v1.0.0 - ASP.NET Core 8.0
