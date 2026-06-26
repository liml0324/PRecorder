# ============================================================
# PRecorder MSIX Packaging Script
# Usage: .\Packaging\build-msix.ps1
#
# Prerequisites:
#   1. Windows 10/11 SDK (includes MakeAppx.exe)
#      https://developer.microsoft.com/windows/downloads/windows-sdk/
#   2. .NET 10 SDK
# ============================================================

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDir = "$projectRoot\bin\publish"
$packageDir = "$projectRoot\bin\package"
$assetsDir = "$packageDir\Assets"

Write-Host "=== Step 1/4: Publish self-contained app ===" -ForegroundColor Cyan
dotnet publish $projectRoot\PRecorder.csproj `
    -c $Configuration `
    -o $publishDir `
    --self-contained `
    -r win-x64 `
    -p:PublishSingleFile=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet publish failed" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "=== Step 2/4: Prepare package layout ===" -ForegroundColor Cyan
if (Test-Path $packageDir) { Remove-Item $packageDir -Recurse -Force }
New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null

Copy-Item "$publishDir\*" $packageDir -Recurse -Exclude "*.pdb"
Copy-Item "$PSScriptRoot\Package.appxmanifest" "$packageDir\AppxManifest.xml" -Force

$iconSource = "$projectRoot\icon.png"
if (Test-Path $iconSource) {
    Copy-Item $iconSource "$assetsDir\StoreLogo.png"
    Copy-Item $iconSource "$assetsDir\Square150x150Logo.png"
    Copy-Item $iconSource "$assetsDir\Square44x44Logo.png"
    Copy-Item $iconSource "$assetsDir\Wide310x150Logo.png"
    Copy-Item $iconSource "$assetsDir\SplashScreen.png"
    Write-Host "  Assets copied from icon.png"
} else {
    Write-Host "  WARNING: icon.png not found, skipping assets" -ForegroundColor Yellow
}

Write-Host "=== Step 3/4: Build MSIX package ===" -ForegroundColor Cyan
$makeAppx = Get-Command "MakeAppx.exe" -ErrorAction SilentlyContinue
if (-not $makeAppx) {
    $sdkPaths = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\MakeAppx.exe",
        "${env:ProgramFiles}\Windows Kits\10\bin\*\x64\MakeAppx.exe"
    )
    $makeAppx = Get-ChildItem $sdkPaths -ErrorAction SilentlyContinue | Select-Object -First 1
}

if (-not $makeAppx) {
    Write-Host "ERROR: MakeAppx.exe not found." -ForegroundColor Red
    Write-Host "Install Windows SDK from:" -ForegroundColor Yellow
    Write-Host "https://developer.microsoft.com/windows/downloads/windows-sdk/" -ForegroundColor Yellow
    exit 1
}
Write-Host "  Found: $makeAppx"

$msixOutput = "$projectRoot\bin\PRecorder_$Version.msix"

& $makeAppx pack /d $packageDir /p $msixOutput /o

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: MakeAppx failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "=== Step 4/4: Signing (optional) ===" -ForegroundColor Cyan
Write-Host "  Skipped. To sign, run:"
Write-Host "  Signtool sign /fd SHA256 /a /f cert.pfx /p PASSWORD $msixOutput"

Write-Host "=== Done! ===" -ForegroundColor Green
Write-Host "  Package: $msixOutput"
Write-Host "  Install: double-click the .msix file"
