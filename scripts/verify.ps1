# Reproducible verification: locked restore, Release build, tests, publish, packaged smoke test.
$ErrorActionPreference = "Stop"
dotnet restore Sitrep.slnx --locked-mode
dotnet build Sitrep.slnx -c Release --no-restore
dotnet test tests/Sitrep.Tests/Sitrep.Tests.csproj -c Release --no-build
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/publish.ps1
$exe = "out/Sitrep-win-x64/Sitrep.exe"
& $exe --self-test
if ($LASTEXITCODE -ne 0) { throw "packaged self-test failed" }
& $exe --diagnose-image MapCords.png --crop "900,640,300,180"
if ($LASTEXITCODE -ne 0) { throw "diagnose A failed" }
& $exe --diagnose-image MapPing.png --crop "900,300,300,160"
if ($LASTEXITCODE -ne 0) { throw "diagnose B failed" }
Write-Host "VERIFY OK"
