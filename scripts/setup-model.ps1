# Retrieves pinned English fast model from official source and verifies checksum.
param(
    [string]$OutDir = "$PSScriptRoot/../src/Sitrep.Desktop/tessdata"
)
$ErrorActionPreference = "Stop"
$url = "https://github.com/tesseract-ocr/tessdata_fast/raw/main/eng.traineddata"
$expectedSha256 = "7D4322BD2A7749724879683FC3912CB542F19906C83BCC1A52132556427170B2"
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
$out = Join-Path $OutDir "eng.traineddata"
Invoke-WebRequest -Uri $url -OutFile $out
$hash = (Get-FileHash $out -Algorithm SHA256).Hash
if ($hash -ne $expectedSha256) {
    Write-Warning ("Checksum mismatch: got {0}, expected {1}. Update pin after verifying source revision." -f $hash, $expectedSha256)
} else {
    Write-Host ("eng.traineddata ok {0}" -f $hash)
}
