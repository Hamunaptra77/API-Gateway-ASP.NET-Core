FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project file
COPY api-gateway-aspnet.csproj .

# Restore dependencies
RUN dotnet restore "api-gateway-aspnet.csproj"

# Copy source code
COPY . .

# Build
RUN dotnet build "api-gateway-aspnet.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "api-gateway-aspnet.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create log directory
RUN mkdir -p /app/logs

# Copy published app
COPY --from=builder /app/publish .

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080

# Run
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
