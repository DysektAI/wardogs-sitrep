# Generates multi-resolution Assets/app.ico and Assets/logo-64.png from the master logo.
# The master (SITREP.png at the repo root, ~1.3 MB) is kept local and gitignored; the generated
# assets are committed, so this only needs to run again when the master artwork changes.
param(
  [string]$Source = "$PSScriptRoot/../SITREP.png",
  [string]$DestDir = "$PSScriptRoot/../src/Sitrep.Desktop/Assets"
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName PresentationCore

if (-not (Test-Path -LiteralPath $Source)) {
  throw "Source image not found: $Source"
}

if (-not (Test-Path -LiteralPath $DestDir)) {
  New-Item -ItemType Directory -Path $DestDir -Force | Out-Null
}

$logoPath = Join-Path $DestDir "logo-64.png"
$icoPath = Join-Path $DestDir "app.ico"
# Both assets are written and verified as temporaries first, so a failure never leaves a mixed or truncated pair.
$logoTemp = "$logoPath.tmp"
$icoTemp = "$icoPath.tmp"
$icoSizes = @(16, 24, 32, 48, 64, 128, 256)

function Resize-Bmp([System.Drawing.Bitmap]$orig, [int]$w, [int]$h) {
  $dest = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  try {
    $g = [System.Drawing.Graphics]::FromImage($dest)
    try {
      $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
      $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
      $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
      $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
      $g.Clear([System.Drawing.Color]::Transparent)
      $g.DrawImage($orig, 0, 0, $w, $h)
    } finally {
      $g.Dispose()
    }
    return $dest
  } catch {
    $dest.Dispose()
    throw
  }
}

function Get-PngBytes([System.Drawing.Bitmap]$orig, [int]$size) {
  $bmp = Resize-Bmp $orig $size $size
  try {
    $ms = New-Object System.IO.MemoryStream
    try {
      $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
      return ,($ms.ToArray())
    } finally {
      $ms.Dispose()
    }
  } finally {
    $bmp.Dispose()
  }
}

$srcImage = [System.Drawing.Bitmap]::FromFile((Resolve-Path $Source).Path)
try {
  # Header logo badge (MainWindow.xaml). Only assets referenced by the app are generated and embedded.
  [System.IO.File]::WriteAllBytes($logoTemp, (Get-PngBytes $srcImage 64))

  # Build multi-resolution .ico (16, 24, 32, 48, 64, 128, 256)
  $pngDataList = @()
  foreach ($s in $icoSizes) {
    $pngDataList += ,(Get-PngBytes $srcImage $s)
  }

  $icoStream = New-Object System.IO.MemoryStream
  $bw = New-Object System.IO.BinaryWriter($icoStream)
  try {
    # Header: reserved (0), type (1 = icon), count
    $bw.Write([UInt16]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]$icoSizes.Count)

    # Directory entries (16 bytes each)
    $dataOffset = 6 + ($icoSizes.Count * 16)
    for ($i = 0; $i -lt $icoSizes.Count; $i++) {
      $size = $icoSizes[$i]
      $data = $pngDataList[$i]
      $dimByte = if ($size -ge 256) { [byte]0 } else { [byte]$size }

      $bw.Write($dimByte)              # bWidth
      $bw.Write($dimByte)              # bHeight
      $bw.Write([byte]0)               # bColorCount
      $bw.Write([byte]0)               # bReserved
      $bw.Write([UInt16]1)             # wPlanes
      $bw.Write([UInt16]32)            # wBitCount
      $bw.Write([UInt32]$data.Length)  # dwBytesInRes
      $bw.Write([UInt32]$dataOffset)   # dwImageOffset

      $dataOffset += $data.Length
    }

    # Image data chunks
    for ($i = 0; $i -lt $icoSizes.Count; $i++) {
      $bw.Write($pngDataList[$i])
    }
    $bw.Flush()
    [System.IO.File]::WriteAllBytes($icoTemp, $icoStream.ToArray())
  } finally {
    $bw.Dispose()
    $icoStream.Dispose()
  }

  # Verify the temporary icon before replacing anything.
  $verifyStream = [System.IO.File]::OpenRead($icoTemp)
  try {
    $decoder = New-Object System.Windows.Media.Imaging.IconBitmapDecoder(
      $verifyStream,
      [System.Windows.Media.Imaging.BitmapCreateOptions]::None,
      [System.Windows.Media.Imaging.BitmapCacheOption]::Default
    )
    if ($decoder.Frames.Count -ne $icoSizes.Count) {
      throw "Decoded frame count ($($decoder.Frames.Count)) does not match expected ($($icoSizes.Count))."
    }
  } finally {
    $verifyStream.Dispose()
  }

  Move-Item -LiteralPath $icoTemp -Destination $icoPath -Force
  Move-Item -LiteralPath $logoTemp -Destination $logoPath -Force
  Write-Host "Successfully generated icon and logo assets in $DestDir"
} finally {
  $srcImage.Dispose()
  Remove-Item -LiteralPath $logoTemp, $icoTemp -Force -ErrorAction SilentlyContinue
}
