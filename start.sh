#!/bin/bash

# API Gateway Quick Start Script
# Für Linux/Mac

set -e

PROJECT_NAME="api-gateway-aspnet"
PROJECT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "================================"
echo "API Gateway - Quick Start"
echo "================================"

# 1. Restore
echo -e "\n📦 Restoring packages..."
cd "$PROJECT_DIR"
dotnet restore

# 2. Build
echo -e "\n🔨 Building project..."
dotnet build --configuration Release

# 3. Run
echo -e "\n🚀 Starting API Gateway..."
echo "📍 Dashboard: http://localhost:5000/dashboard"
echo "📍 Health:    http://localhost:5000/health"
echo "📍 API Info:  http://localhost:5000/api-info"
echo ""

dotnet run --configuration Release

echo -e "\n✅ Done!"
