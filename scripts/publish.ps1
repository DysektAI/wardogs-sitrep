# Publishes win-x64 self-contained folder and zips for local handoff.
$ErrorActionPreference = "Stop"
$outDir = "out/WardogsMortarAssist-win-x64"
$zip = "out/WardogsMortarAssist-win-x64.zip"
Remove-Item -Recurse -Force $outDir, $zip -ErrorAction SilentlyContinue
dotnet publish src/WardogsMortar.Desktop/WardogsMortar.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=false -o $outDir
Compress-Archive -Path "$outDir/*" -DestinationPath $zip
Write-Host ("Published {0} + {1}" -f $outDir, $zip)
