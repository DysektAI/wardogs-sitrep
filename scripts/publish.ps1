# Locked, self-contained folder + ZIP; model must pass verification before publishing.
. "$PSScriptRoot/common.ps1"
Push-Location "$PSScriptRoot/.."
try {
  & "$PSScriptRoot/setup-model.ps1"
  $outDir = 'out/Sitrep-win-x64'
  $zip = 'out/Sitrep-win-x64.zip'
  foreach ($path in @($outDir, $zip)) {
    if (Test-Path $path) { Remove-Item -LiteralPath $path -Recurse -Force }
  }
  Invoke-CheckedNative dotnet @('restore', 'src/Sitrep.Desktop/Sitrep.Desktop.csproj', '--locked-mode', '-r', 'win-x64', '-p:SelfContained=true')
  Invoke-CheckedNative dotnet @('publish', 'src/Sitrep.Desktop/Sitrep.Desktop.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '--no-restore', '-p:PublishSingleFile=false', '-o', $outDir)
  Compress-Archive -Path "$outDir/*" -DestinationPath $zip
  Write-Host "Published $outDir + $zip"
} finally {
  Pop-Location
}
