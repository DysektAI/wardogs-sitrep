# Canonical clean-clone gate. Private screenshots are an explicit additional gate.
param([switch]$PrivateFixtures)
. "$PSScriptRoot/common.ps1"
Push-Location "$PSScriptRoot/.."
try {
  & "$PSScriptRoot/setup-model.ps1"
  Invoke-CheckedNative dotnet @('restore', 'Sitrep.slnx', '--locked-mode')
  Invoke-CheckedNative dotnet @('build', 'Sitrep.slnx', '-c', 'Release', '--no-restore')
  Invoke-CheckedNative dotnet @('test', 'tests/Sitrep.Tests/Sitrep.Tests.csproj', '-c', 'Release', '--no-build')
  & "$PSScriptRoot/publish.ps1"
  $exe = (Resolve-Path 'out/Sitrep-win-x64/Sitrep.exe').Path
  Invoke-PackagedCheck $exe @('--self-test')
  Invoke-PackagedCheck $exe @('--integration-test')
  & "$PSScriptRoot/test-verification.ps1" -Exe $exe
  $negatives = @(Get-ChildItem 'tests/fixtures-neg/*.png')
  if ($negatives.Count -ne 5) { throw 'Expected all five committed negative fixtures.' }
  foreach ($image in $negatives) {
    $report = Join-Path $PWD "out/negative-$($image.BaseName).json"
    if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report }
    Invoke-PackagedCheck $exe @('--diagnose-image', $image.FullName, '--report', $report) -ExpectedExit 2
    $result = Get-Content $report -Raw | ConvertFrom-Json
    if ($result.success -or $result.rejection -in @('OCR_UNAVAILABLE', 'BAD_CROP')) {
      throw "Negative fixture did not exercise OCR rejection: $image"
    }
  }
  if ($PrivateFixtures) {
    foreach ($fixture in @(
      @{ Name = 'MapCords.png'; Crop = '900,640,300,180'; X = 101.53; Y = 107.77 },
      @{ Name = 'MapPing.png'; Crop = '900,300,300,160'; X = 101.66; Y = 110.10 }
    )) {
      $image = (Resolve-Path $fixture.Name).Path
      $report = Join-Path $PWD "out/$($fixture.Name).json"
      if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report }
      Invoke-PackagedCheck $exe @('--diagnose-image', $image, '--crop', $fixture.Crop, '--report', $report)
      $result = Get-Content $report -Raw | ConvertFrom-Json
      if (-not $result.success -or $result.x -ne $fixture.X -or $result.y -ne $fixture.Y) {
        throw "Wrong coordinates for $image"
      }
    }
  }
  Write-Host "VERIFY OK (private fixtures: $PrivateFixtures)"
} finally {
  Pop-Location
}
