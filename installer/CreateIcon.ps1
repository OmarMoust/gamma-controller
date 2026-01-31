# Create a proper .ico file for the installer
Add-Type -AssemblyName System.Drawing

$sizes = @(16, 32, 48, 256)
$iconPath = Join-Path $PSScriptRoot "GammaController.ico"

# Create bitmaps for each size
$bitmaps = @()
foreach ($size in $sizes) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    
    # Scale factor
    $scale = $size / 32.0
    
    # Draw sun circle (yellow/gold)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 200, 50))
    $circleSize = 16 * $scale
    $circlePos = 8 * $scale
    $g.FillEllipse($brush, $circlePos, $circlePos, $circleSize, $circleSize)
    
    # Draw rays
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 255, 200, 50), (2 * $scale))
    $center = 16 * $scale
    
    # Top ray
    $g.DrawLine($pen, $center, 2 * $scale, $center, 6 * $scale)
    # Bottom ray
    $g.DrawLine($pen, $center, 26 * $scale, $center, 30 * $scale)
    # Left ray
    $g.DrawLine($pen, 2 * $scale, $center, 6 * $scale, $center)
    # Right ray
    $g.DrawLine($pen, 26 * $scale, $center, 30 * $scale, $center)
    # Diagonal rays
    $g.DrawLine($pen, 6 * $scale, 6 * $scale, 9 * $scale, 9 * $scale)
    $g.DrawLine($pen, 23 * $scale, 23 * $scale, 26 * $scale, 26 * $scale)
    $g.DrawLine($pen, 26 * $scale, 6 * $scale, 23 * $scale, 9 * $scale)
    $g.DrawLine($pen, 6 * $scale, 26 * $scale, 9 * $scale, 23 * $scale)
    
    $g.Dispose()
    $brush.Dispose()
    $pen.Dispose()
    
    $bitmaps += $bitmap
}

# Save the 256x256 as a PNG first, then we'll need to create the ICO manually
# For simplicity, just save the 32x32 as the icon
$icon = [System.Drawing.Icon]::FromHandle($bitmaps[1].GetHicon())

# Save using a file stream
$fs = [System.IO.File]::Create($iconPath)
$icon.Save($fs)
$fs.Close()

# Cleanup
foreach ($bmp in $bitmaps) {
    $bmp.Dispose()
}

Write-Host "Icon created at: $iconPath"


