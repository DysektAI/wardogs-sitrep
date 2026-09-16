# Exercises failure paths with real native/packaged processes, isolated from user config/models.
param([Parameter(Mandatory)][string]$Exe)
. "$PSScriptRoot/common.ps1"

function Assert-Throws {
  param([scriptblock]$Action, [string]$ExpectedMessage)
  try { & $Action } catch {
    if ($_.Exception.Message -like "*$ExpectedMessage*") { return }
    throw
  }
  throw "Expected failure containing: $ExpectedMessage"
}

$temp = Join-Path ([System.IO.Path]::GetTempPath()) "Sitrep-gate-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $temp | Out-Null
try {
  Assert-Throws { Invoke-CheckedNative dotnet @('--sitrep-intentionally-invalid-option') } 'failed with exit code'
  Assert-Throws { Invoke-PackagedCheck $Exe @('--diagnose-image', (Join-Path $temp 'missing.png')) } 'exited 1, expected 0'
  # A real GUI-subsystem process's nonzero exit must not disappear in PowerShell.
  Invoke-PackagedCheck $Exe @('--diagnose-image', (Join-Path $temp 'missing.png')) -ExpectedExit 1
  $model = Join-Path $temp 'eng.traineddata'
  [System.IO.File]::WriteAllText($model, 'not a valid model')
  Assert-Throws { & "$PSScriptRoot/setup-model.ps1" -OutDir $temp } 'Invalid model checksum'
  if ([System.IO.File]::ReadAllText($model) -ne 'not a valid model') { throw 'Setup silently replaced a corrupt cached model.' }
  # Use an isolated copy, not the generated artifact or the user's installed files.
  $package = Join-Path $temp 'package'
  Copy-Item -LiteralPath (Split-Path $Exe) -Destination $package -Recurse
  $testExe = Join-Path $package 'Sitrep.exe'
  Remove-Item -LiteralPath (Join-Path $package 'tessdata/eng.traineddata')
  Invoke-PackagedCheck $testExe @('--self-test') -ExpectedExit 2
  [System.IO.File]::WriteAllText((Join-Path $package 'data/l81-apollyon.json'), '{"weaponId":42}')
  Invoke-PackagedCheck $testExe @('--self-test') -ExpectedExit 1
  Write-Host 'Verification failure-path checks passed (native, GUI exit, corrupt cached model, missing model, corrupt table).'
} finally {
  Remove-Item -LiteralPath $temp -Recurse -Force
}
