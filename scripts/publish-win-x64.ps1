param(
    [switch]$SkipTests,
    [switch]$BuildInstaller
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root "HardwarePaintShopSystem.sln"
$desktop = Join-Path $root "src\HardwarePaintShop.Desktop\HardwarePaintShop.Desktop.csproj"
$api = Join-Path $root "src\HardwarePaintShop.Api\HardwarePaintShop.Api.csproj"
$publish = Join-Path $root "artifacts\publish\win-x64"
$apiPublish = Join-Path $publish "Api"
$installer = Join-Path $root "installer\HardwarePaintShop.iss"

Write-Host "Restoring solution..." -ForegroundColor Cyan
dotnet restore $solution

Write-Host "Building Release..." -ForegroundColor Cyan
dotnet build $solution -c Release --no-restore

if (-not $SkipTests) {
    Write-Host "Running tests..." -ForegroundColor Cyan
    dotnet test $solution -c Release --no-build
}

if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
Write-Host "Publishing self-contained Windows x64 application..." -ForegroundColor Cyan
dotnet publish $desktop -c Release -r win-x64 --self-contained true --no-restore `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publish

Write-Host "Publishing local mobile API..." -ForegroundColor Cyan
dotnet publish $api -c Release -r win-x64 --self-contained true --no-restore `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $apiPublish

Copy-Item (Join-Path $root "README.md") $publish -Force
Write-Host "Publish output: $publish" -ForegroundColor Green

if ($BuildInstaller) {
    $iscc = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if (-not $iscc) {
        $defaultIscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
        if (Test-Path $defaultIscc) { $iscc = $defaultIscc }
    }
    if (-not $iscc) { throw "Inno Setup 6 was not found. Install it or add ISCC.exe to PATH." }
    Write-Host "Building installer..." -ForegroundColor Cyan
    & $iscc $installer
    Write-Host "Installer output: $(Join-Path $root 'artifacts\installer')" -ForegroundColor Green
}
