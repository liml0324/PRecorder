# ============================================================
# PRecorder Packaging Script (Inno Setup)
# Produces a single .exe installer with .NET runtime included.
#
# Usage: .\Packaging\build.ps1 [-Version "1.0.0.0"]
#
# Prerequisites:
#   1. Inno Setup 6 (ISCC.exe)
#      Download: https://jrsoftware.org/isinfo.php
#   2. .NET 10 SDK
# ============================================================

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDir = "$projectRoot\bin\publish_full"
$setupExe = "$projectRoot\bin\PRecorder_Setup_${Version}_x64.exe"

Write-Host "=== Publish (self-contained) ===" -ForegroundColor Cyan
dotnet publish $projectRoot\PRecorder.csproj `
    -c $Configuration `
    -o $publishDir `
    --self-contained `
    -r win-x64 `
    -p:PublishSingleFile=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: publish failed" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "=== Build Installer (Inno Setup) ===" -ForegroundColor Cyan

# Locate ISCC.exe
$iscc = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
if (-not $iscc) {
    $isccPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe"
    )
    $iscc = Get-ChildItem $isccPaths -ErrorAction SilentlyContinue | Select-Object -First 1
}

if (-not $iscc) {
    Write-Host "ERROR: Inno Setup not found." -ForegroundColor Red
    Write-Host "Install: winget install JRSoftware.InnoSetup" -ForegroundColor Yellow
    Write-Host "Or: https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
    exit 1
}

Write-Host "  ISCC: $iscc"
& $iscc /Qp "$PSScriptRoot\setup.iss"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: ISCC failed (code $LASTEXITCODE)" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "=== Done! ===" -ForegroundColor Green
Write-Host "  $setupExe"
Write-Host "  Size: $('{0:N1}' -f ((Get-Item $setupExe).Length / 1MB)) MB"
