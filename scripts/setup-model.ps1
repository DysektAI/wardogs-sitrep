# Official English fast model: immutable revision plus fail-closed checksum verification.
param([string]$OutDir = "$PSScriptRoot/../src/Sitrep.Desktop/tessdata")
$ErrorActionPreference = 'Stop'
$url = 'https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/87416418657359cb625c412a48b6e1d6d41c29bd/eng.traineddata'
$expectedSha256 = '7D4322BD2A7749724879683FC3912CB542F19906C83BCC1A52132556427170B2'
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
$out = Join-Path $OutDir 'eng.traineddata'
if (Test-Path -LiteralPath $out) {
  if ((Get-FileHash -LiteralPath $out -Algorithm SHA256).Hash -ne $expectedSha256) {
    throw "Invalid model checksum at $out. Remove that file and rerun setup-model.ps1; do not use it."
  }
  Write-Host "eng.traineddata verified (cached): $expectedSha256"
  return
}
$temp = "$out.$([guid]::NewGuid().ToString('N')).download"
try {
  Invoke-WebRequest -Uri $url -OutFile $temp
  $hash = (Get-FileHash -LiteralPath $temp -Algorithm SHA256).Hash
  if ($hash -ne $expectedSha256) {
    throw "Model checksum mismatch: got $hash, expected $expectedSha256. Download discarded."
  }
  Move-Item -LiteralPath $temp -Destination $out
  Write-Host "eng.traineddata verified: $hash"
} finally {
  if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Force }
}
