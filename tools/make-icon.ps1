# Draws the app logo (green ring + "T") and writes a multi-size .ico.
# Usage: powershell -ExecutionPolicy Bypass -File tools\make-icon.ps1
param([string]$Out = "$PSScriptRoot\..\src\TarekGamingSpace\Assets\app.ico")

Add-Type -AssemblyName System.Drawing

function New-LogoPng([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = $size / 24.0
    $fill = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 11, 15, 13))
    $ring = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 82, 176, 67)), ([Math]::Max(1.2, 1.9 * $s))
    $tPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 230, 239, 232)), ([Math]::Max(1.5, 2.6 * $s))
    $tPen.StartCap = 'Round'; $tPen.EndCap = 'Round'
    $g.FillEllipse($fill, 1.2 * $s, 1.2 * $s, 21.6 * $s, 21.6 * $s)
    $g.DrawEllipse($ring, 2 * $s, 2 * $s, 20 * $s, 20 * $s)
    $g.DrawLine($tPen, 7.5 * $s, 8 * $s, 16.5 * $s, 8 * $s)
    $g.DrawLine($tPen, 12 * $s, 8 * $s, 12 * $s, 17 * $s)
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return , $ms.ToArray()
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$pngs = $sizes | ForEach-Object { , (New-LogoPng $_) }

$fs = [System.IO.File]::Create((Resolve-Path -LiteralPath (Split-Path $Out)).Path + "\" + (Split-Path $Out -Leaf))
$w = New-Object System.IO.BinaryWriter $fs
$w.Write([UInt16]0); $w.Write([UInt16]1); $w.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $d = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $w.Write([byte]$d); $w.Write([byte]$d); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([UInt16]1); $w.Write([UInt16]32)
    $w.Write([UInt32]$pngs[$i].Length); $w.Write([UInt32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $w.Write($p) }
$w.Close()
Write-Host "Wrote $Out"
