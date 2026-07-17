
# =============================================================
# HardwarePaintShopSystem - Solution Setup Script
# =============================================================
$ErrorActionPreference = "Stop"
$root = "d:\New folder (2)\HardwarePaintShopSystem"
Set-Location $root

Write-Host "`n== Creating Solution ==" -ForegroundColor Cyan
dotnet new sln -n HardwarePaintShopSystem --force

# =============================================================
# Create Projects
# =============================================================
Write-Host "`n== Creating Projects ==" -ForegroundColor Cyan

dotnet new wpf        -n HardwarePaintShop.Desktop        -o src/HardwarePaintShop.Desktop        --framework net8.0-windows
dotnet new classlib   -n HardwarePaintShop.Domain          -o src/HardwarePaintShop.Domain          --framework net8.0
dotnet new classlib   -n HardwarePaintShop.Application     -o src/HardwarePaintShop.Application     --framework net8.0
dotnet new classlib   -n HardwarePaintShop.Infrastructure  -o src/HardwarePaintShop.Infrastructure  --framework net8.0
dotnet new webapi     -n HardwarePaintShop.Api             -o src/HardwarePaintShop.Api             --framework net8.0
dotnet new xunit      -n HardwarePaintShop.UnitTests       -o tests/HardwarePaintShop.UnitTests     --framework net8.0
dotnet new xunit      -n HardwarePaintShop.IntegrationTests-o tests/HardwarePaintShop.IntegrationTests --framework net8.0

# =============================================================
# Add Projects to Solution
# =============================================================
Write-Host "`n== Adding Projects to Solution ==" -ForegroundColor Cyan

dotnet sln add src/HardwarePaintShop.Desktop/HardwarePaintShop.Desktop.csproj
dotnet sln add src/HardwarePaintShop.Domain/HardwarePaintShop.Domain.csproj
dotnet sln add src/HardwarePaintShop.Application/HardwarePaintShop.Application.csproj
dotnet sln add src/HardwarePaintShop.Infrastructure/HardwarePaintShop.Infrastructure.csproj
dotnet sln add src/HardwarePaintShop.Api/HardwarePaintShop.Api.csproj
dotnet sln add tests/HardwarePaintShop.UnitTests/HardwarePaintShop.UnitTests.csproj
dotnet sln add tests/HardwarePaintShop.IntegrationTests/HardwarePaintShop.IntegrationTests.csproj

# =============================================================
# Project References
# =============================================================
Write-Host "`n== Adding Project References ==" -ForegroundColor Cyan

# Application -> Domain
dotnet add src/HardwarePaintShop.Application/HardwarePaintShop.Application.csproj reference src/HardwarePaintShop.Domain/HardwarePaintShop.Domain.csproj

# Infrastructure -> Application + Domain
dotnet add src/HardwarePaintShop.Infrastructure/HardwarePaintShop.Infrastructure.csproj reference src/HardwarePaintShop.Application/HardwarePaintShop.Application.csproj
dotnet add src/HardwarePaintShop.Infrastructure/HardwarePaintShop.Infrastructure.csproj reference src/HardwarePaintShop.Domain/HardwarePaintShop.Domain.csproj

# Desktop -> Application + Domain + Infrastructure
dotnet add src/HardwarePaintShop.Desktop/HardwarePaintShop.Desktop.csproj reference src/HardwarePaintShop.Application/HardwarePaintShop.Application.csproj
dotnet add src/HardwarePaintShop.Desktop/HardwarePaintShop.Desktop.csproj reference src/HardwarePaintShop.Domain/HardwarePaintShop.Domain.csproj
dotnet add src/HardwarePaintShop.Desktop/HardwarePaintShop.Desktop.csproj reference src/HardwarePaintShop.Infrastructure/HardwarePaintShop.Infrastructure.csproj

# Api -> Application + Domain + Infrastructure
dotnet add src/HardwarePaintShop.Api/HardwarePaintShop.Api.csproj reference src/HardwarePaintShop.Application/HardwarePaintShop.Application.csproj
dotnet add src/HardwarePaintShop.Api/HardwarePaintShop.Api.csproj reference src/HardwarePaintShop.Domain/HardwarePaintShop.Domain.csproj
dotnet add src/HardwarePaintShop.Api/HardwarePaintShop.Api.csproj reference src/HardwarePaintShop.Infrastructure/HardwarePaintShop.Infrastructure.csproj

# Tests
dotnet add tests/HardwarePaintShop.UnitTests/HardwarePaintShop.UnitTests.csproj reference src/HardwarePaintShop.Application/HardwarePaintShop.Application.csproj
dotnet add tests/HardwarePaintShop.UnitTests/HardwarePaintShop.UnitTests.csproj reference src/HardwarePaintShop.Domain/HardwarePaintShop.Domain.csproj
dotnet add tests/HardwarePaintShop.IntegrationTests/HardwarePaintShop.IntegrationTests.csproj reference src/HardwarePaintShop.Infrastructure/HardwarePaintShop.Infrastructure.csproj
dotnet add tests/HardwarePaintShop.IntegrationTests/HardwarePaintShop.IntegrationTests.csproj reference src/HardwarePaintShop.Domain/HardwarePaintShop.Domain.csproj

# =============================================================
# NuGet Packages - Infrastructure
# =============================================================
Write-Host "`n== Installing NuGet Packages ==" -ForegroundColor Cyan

$infra = "src/HardwarePaintShop.Infrastructure/HardwarePaintShop.Infrastructure.csproj"
dotnet add $infra package Microsoft.EntityFrameworkCore
dotnet add $infra package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add $infra package Microsoft.Extensions.DependencyInjection
dotnet add $infra package Microsoft.Extensions.Configuration.Json
dotnet add $infra package Serilog.Sinks.File

# Application
$app = "src/HardwarePaintShop.Application/HardwarePaintShop.Application.csproj"
dotnet add $app package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add $app package BCrypt.Net-Next

# Desktop
$desktop = "src/HardwarePaintShop.Desktop/HardwarePaintShop.Desktop.csproj"
dotnet add $desktop package CommunityToolkit.Mvvm
dotnet add $desktop package Microsoft.Extensions.DependencyInjection
dotnet add $desktop package Microsoft.Extensions.Configuration.Json
dotnet add $desktop package Serilog.Sinks.File

Write-Host "`n== Packages installed ==" -ForegroundColor Green
