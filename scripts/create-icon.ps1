$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$assets = Join-Path $repoRoot "assets"
$iconPath = Join-Path $assets "app-icon.ico"

New-Item -ItemType Directory -Force -Path $assets | Out-Null

Add-Type -AssemblyName System.Drawing

function New-RoundedRectPath {
  param(
    [System.Drawing.RectangleF]$Rect,
    [float]$Radius
  )

  $path = New-Object System.Drawing.Drawing2D.GraphicsPath
  $diameter = $Radius * 2
  $path.AddArc($Rect.X, $Rect.Y, $diameter, $diameter, 180, 90)
  $path.AddArc($Rect.Right - $diameter, $Rect.Y, $diameter, $diameter, 270, 90)
  $path.AddArc($Rect.Right - $diameter, $Rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
  $path.AddArc($Rect.X, $Rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
  $path.CloseFigure()
  return $path
}

function New-IconDibBytes {
  param([int]$Size)

  $bmp = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.Clear([System.Drawing.Color]::Transparent)

  $scale = $Size / 256.0
  $rect = New-Object System.Drawing.RectangleF (10 * $scale), (10 * $scale), (236 * $scale), (236 * $scale)
  $path = New-RoundedRectPath $rect (46 * $scale)

  $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(28, 116, 100)), ([System.Drawing.Color]::FromArgb(35, 39, 42)), 45
  $g.FillPath($bg, $path)

  $stripePath = New-Object System.Drawing.Drawing2D.GraphicsPath
  $stripePath.AddPolygon(@(
    (New-Object System.Drawing.PointF (28 * $scale), (188 * $scale)),
    (New-Object System.Drawing.PointF (192 * $scale), (28 * $scale)),
    (New-Object System.Drawing.PointF (236 * $scale), (64 * $scale)),
    (New-Object System.Drawing.PointF (72 * $scale), (224 * $scale))
  ))
  $stripeBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(38, 255, 255, 255))
  $g.FillPath($stripeBrush, $stripePath)

  $white = [System.Drawing.Color]::FromArgb(250, 255, 255, 255)
  $soft = [System.Drawing.Color]::FromArgb(180, 255, 255, 255)

  $arrowPen = New-Object System.Drawing.Pen $white, (18 * $scale)
  $arrowPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
  $arrowPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
  $g.DrawLine($arrowPen, (128 * $scale), (172 * $scale), (128 * $scale), (78 * $scale))

  $headPen = New-Object System.Drawing.Pen $white, (16 * $scale)
  $headPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
  $headPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
  $g.DrawLine($headPen, (128 * $scale), (78 * $scale), (88 * $scale), (118 * $scale))
  $g.DrawLine($headPen, (128 * $scale), (78 * $scale), (168 * $scale), (118 * $scale))

  $trayPen = New-Object System.Drawing.Pen $white, (14 * $scale)
  $trayPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
  $trayPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
  $g.DrawLine($trayPen, (78 * $scale), (184 * $scale), (178 * $scale), (184 * $scale))

  $branchPen = New-Object System.Drawing.Pen $soft, (8 * $scale)
  $branchPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
  $branchPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
  $g.DrawLine($branchPen, (76 * $scale), (80 * $scale), (76 * $scale), (150 * $scale))
  $g.DrawLine($branchPen, (76 * $scale), (150 * $scale), (104 * $scale), (168 * $scale))
  $nodeBrush = New-Object System.Drawing.SolidBrush $soft
  foreach ($point in @(
    @(76, 80),
    @(76, 150),
    @(104, 168)
  )) {
    $g.FillEllipse($nodeBrush, (($point[0] - 8) * $scale), (($point[1] - 8) * $scale), (16 * $scale), (16 * $scale))
  }

  $ms = New-Object System.IO.MemoryStream
  $writer = New-Object System.IO.BinaryWriter $ms
  $writer.Write([UInt32]40)
  $writer.Write([Int32]$Size)
  $writer.Write([Int32]($Size * 2))
  $writer.Write([UInt16]1)
  $writer.Write([UInt16]32)
  $writer.Write([UInt32]0)
  $writer.Write([UInt32]($Size * $Size * 4))
  $writer.Write([Int32]0)
  $writer.Write([Int32]0)
  $writer.Write([UInt32]0)
  $writer.Write([UInt32]0)

  for ($y = $Size - 1; $y -ge 0; $y--) {
    for ($x = 0; $x -lt $Size; $x++) {
      $pixel = $bmp.GetPixel($x, $y)
      $writer.Write([byte]$pixel.B)
      $writer.Write([byte]$pixel.G)
      $writer.Write([byte]$pixel.R)
      $writer.Write([byte]$pixel.A)
    }
  }

  $maskRowBytes = [int]([Math]::Ceiling($Size / 32.0) * 4)
  $mask = New-Object byte[] ($maskRowBytes * $Size)
  $writer.Write($mask)
  $writer.Flush()
  $bytes = $ms.ToArray()

  $arrowPen.Dispose()
  $headPen.Dispose()
  $trayPen.Dispose()
  $branchPen.Dispose()
  $nodeBrush.Dispose()
  $stripeBrush.Dispose()
  $stripePath.Dispose()
  $bg.Dispose()
  $path.Dispose()
  $g.Dispose()
  $bmp.Dispose()
  $writer.Dispose()
  $ms.Dispose()

  return ,$bytes
}

$sizes = @(256, 128, 64, 48, 32, 16)
$images = @()
foreach ($size in $sizes) {
  $images += [pscustomobject]@{
    Size = $size
    Bytes = New-IconDibBytes $size
  }
}

$fs = [System.IO.File]::Create($iconPath)
$writer = New-Object System.IO.BinaryWriter $fs
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$images.Count)

$offset = 6 + (16 * $images.Count)
foreach ($image in $images) {
  $dimension = if ($image.Size -ge 256) { 0 } else { $image.Size }
  $writer.Write([byte]$dimension)
  $writer.Write([byte]$dimension)
  $writer.Write([byte]0)
  $writer.Write([byte]0)
  $writer.Write([UInt16]1)
  $writer.Write([UInt16]32)
  $writer.Write([UInt32]$image.Bytes.Length)
  $writer.Write([UInt32]$offset)
  $offset += $image.Bytes.Length
}

foreach ($image in $images) {
  $writer.Write($image.Bytes)
}

$writer.Dispose()
$fs.Dispose()

Write-Host "Created: $iconPath"
