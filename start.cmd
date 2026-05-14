@echo off
REM API Gateway Quick Start Script für Windows

setlocal enabledelayedexpansion

set PROJECT_NAME=api-gateway-aspnet
set PROJECT_DIR=%~dp0

echo.
echo ================================
echo API Gateway - Quick Start
echo ================================

REM 1. Restore
echo.
echo 📦 Restoring packages...
cd /d "%PROJECT_DIR%"
call dotnet restore
if !errorlevel! neq 0 (
    echo ❌ Restore failed
    exit /b 1
)

REM 2. Build
echo.
echo 🔨 Building project...
call dotnet build --configuration Release
if !errorlevel! neq 0 (
    echo ❌ Build failed
    exit /b 1
)

REM 3. Run
echo.
echo 🚀 Starting API Gateway...
echo 📍 Dashboard: http://localhost:5000/dashboard
echo 📍 Health:    http://localhost:5000/health
echo 📍 API Info:  http://localhost:5000/api-info
echo.

call dotnet run --configuration Release
if !errorlevel! neq 0 (
    echo ❌ Failed to start
    exit /b 1
)

echo.
echo ✅ Done!
pause
