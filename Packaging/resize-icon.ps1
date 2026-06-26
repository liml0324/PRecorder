# ============================================================
# Resize and optimize icon.png for distribution
# Usage: .\Packaging\resize-icon.ps1
# ============================================================

$projectRoot = Split-Path -Parent $PSScriptRoot
$source = "$projectRoot\icon.png"

if (-not (Test-Path $source)) {
    Write-Host "ERROR: icon.png not found" -ForegroundColor Red
    exit 1
}

Add-Type -AssemblyName System.Drawing

$original = [System.Drawing.Bitmap]::FromFile($source)
Write-Host "Original: $($original.Width)x$($original.Height), $('{0:N1}' -f ((Get-Item $source).Length / 1KB)) KB"

# Output sizes: name, width, height, description
$sizes = @(
    @{Name="icon_256.png"; W=256; H=256; Desc="General use"},
    @{Name="icon_48.png";  W=48;  H=48;  Desc="Window / tray icon"},
    @{Name="icon_32.png";  W=32;  H=32;  Desc="Tray icon (small)"}
)

foreach ($s in $sizes) {
    $outPath = "$projectRoot\$($s.Name)"
    $resized = New-Object System.Drawing.Bitmap($original, $s.W, $s.H)

    # Save with optimal compression
    [System.Drawing.Imaging.EncoderParameters]$encoderParams = New-Object System.Drawing.Imaging.EncoderParameters(1)
    $encoderParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter(
        [System.Drawing.Imaging.Encoder]::Quality, 90L)

    $codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
        Where-Object { $_.MimeType -eq "image/png" } |
        Select-Object -First 1

    $resized.Save($outPath, $codec, $encoderParams)
    $resized.Dispose()

    $kb = (Get-Item $outPath).Length / 1KB
    Write-Host "  $($s.Name): $($s.W)x$($s.H), $('{0:N1}' -f $kb) KB  ($($s.Desc))"
}

# Rename original to keep it
$backupPath = "$projectRoot\icon_original.png"
if (-not (Test-Path $backupPath)) {
    Copy-Item $source $backupPath
    Write-Host "  Original backed up as icon_original.png"
}

$original.Dispose()

Write-Host ""
Write-Host "=== Done! ===" -ForegroundColor Green
Write-Host "  icon_256.png  -> use this for app references (was icon.png)"
Write-Host "  icon_48.png   -> use for window/tray"
Write-Host "  icon_original.png -> original file preserved"