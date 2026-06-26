# ============================================================
# PRecorder Packaging Script (Inno Setup)
# Produces two .exe installers:
#   1. Full:       self-contained, includes .NET (~50 MB)
#   2. Portable:   framework-dependent, needs .NET 10 (~2 MB)
#
# Usage: .\Packaging\build.ps1 [-Version "1.0.0.0"] [-Full] [-Portable]
#
# Prerequisites:
#   1. Inno Setup 6 (ISCC.exe)
#   2. .NET 10 SDK
# ============================================================

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$Full,
    [switch]$Portable
)

if (-not $Full -and -not $Portable) { $Full = $true; $Portable = $true }

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot

# P/Invoke helper for DestroyIcon
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public class NativeMethods {
    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
'@ -ErrorAction SilentlyContinue

# Generate icon.ico from icon_48.png
Add-Type -AssemblyName System.Drawing
$icoPath = "$projectRoot\icon.ico"
$pngPath = "$projectRoot\icon_48.png"
if (-not (Test-Path $icoPath) -or (Get-Item $pngPath).LastWriteTime -gt (Get-Item $icoPath).LastWriteTime) {
    $src = [System.Drawing.Bitmap]::FromFile($pngPath)
    # Fix: convert to Format32bppArgb to prevent GetHicon() RGB/BGR swap
    $bmp = New-Object System.Drawing.Bitmap($src.Width, $src.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.DrawImage($src, 0, 0, $src.Width, $src.Height)
    $g.Dispose(); $src.Dispose()

    $hIcon = $bmp.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($hIcon)
    $fs = [System.IO.File]::Create($icoPath)
    $icon.Save($fs)
    $fs.Close()
    $icon.Dispose(); $bmp.Dispose()
    [NativeMethods]::DestroyIcon($hIcon) | Out-Null
    Write-Host "Generated icon.ico ($((Get-Item $icoPath).Length) bytes)" -ForegroundColor DarkGray
}

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
Write-Host "ISCC: $iscc"

# ============================================================
# Full version (self-contained)
# ============================================================
if ($Full) {
    Write-Host "=== Full: Publish (self-contained) ===" -ForegroundColor Cyan
    dotnet publish $projectRoot\PRecorder.csproj `
        -c $Configuration -o "$projectRoot\bin\publish_full" `
        --self-contained -r win-x64 -p:PublishSingleFile=true
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR" -ForegroundColor Red; exit 1 }

    & $iscc /Qp "$PSScriptRoot\setup.iss"
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: ISCC failed" -ForegroundColor Red; exit 1 }

    $exe = "$projectRoot\bin\PRecorder_Setup_${Version}_x64.exe"
    Write-Host "  -> $exe  ($('{0:N1}' -f ((Get-Item $exe).Length / 1MB)) MB)" -ForegroundColor Green
}

# ============================================================
# Portable version (framework-dependent)
# ============================================================
if ($Portable) {
    Write-Host "=== Portable: Publish (framework-dependent) ===" -ForegroundColor Cyan
    dotnet publish $projectRoot\PRecorder.csproj `
        -c $Configuration -o "$projectRoot\bin\publish_fd" `
        -r win-x64 -p:SelfContained=false -p:PublishSingleFile=false
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR" -ForegroundColor Red; exit 1 }

    # Generate VBS launcher to suppress console window
    $vbsContent = 'CreateObject("WScript.Shell").Run "dotnet.exe """"{0}\PRecorder.dll"""", 0, False' -f "%APP%"
    # Use a placeholder that Inno Setup replaces at install time
    Set-Content "$projectRoot\bin\publish_fd\PRecorder.vbs" -Value @"
Set ws = CreateObject("WScript.Shell")
ws.Run "dotnet.exe """ + ws.ExpandEnvironmentStrings("%ProgramFiles%") + "\..\..\..\..\PRecorder\PRecorder.dll" + """", 0, False
"@
    # Simpler: hardcode the default install path
    $vbs = @'
Set ws = CreateObject("WScript.Shell")
appDir = ws.ExpandEnvironmentStrings("%LOCALAPPDATA%") & "\Programs\PRecorder"
ws.Run "dotnet.exe """ & appDir & "\PRecorder.dll""", 0, False
'@
    Set-Content "$projectRoot\bin\publish_fd\PRecorder.vbs" -Value $vbs

    & $iscc /Qp "$PSScriptRoot\setup_fd.iss"
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: ISCC failed" -ForegroundColor Red; exit 1 }

    $exe = "$projectRoot\bin\PRecorder_Setup_${Version}_x64_fd.exe"
    Write-Host "  -> $exe  ($('{0:N1}' -f ((Get-Item $exe).Length / 1MB)) MB)  - needs .NET 10" -ForegroundColor Green
}

Write-Host "=== Done! ===" -ForegroundColor Green
