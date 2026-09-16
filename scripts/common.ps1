Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-CheckedNative {
  param([string]$Command, [string[]]$Arguments)
  & $Command @Arguments
  if ($LASTEXITCODE -ne 0) {
    throw "$Command failed with exit code $LASTEXITCODE."
  }
}

function Invoke-PackagedCheck {
  param(
    [string]$Exe,
    [string[]]$Arguments,
    [int]$ExpectedExit = 0,
    [string]$WorkingDirectory = [System.IO.Path]::GetTempPath()
  )
  # WinExe does not reliably block PowerShell. Use Process and argument-list escaping,
  # drain both redirected streams asynchronously, wait, then inspect this process's exit.
  $info = [System.Diagnostics.ProcessStartInfo]::new($Exe)
  $info.UseShellExecute = $false
  $info.RedirectStandardOutput = $true
  $info.RedirectStandardError = $true
  $info.WorkingDirectory = $WorkingDirectory
  foreach ($argument in $Arguments) { $info.ArgumentList.Add($argument) }
  $process = [System.Diagnostics.Process]::new()
  $process.StartInfo = $info
  try {
    if (-not $process.Start()) { throw "Could not start $Exe" }
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(120000)) {
      $process.Kill($true)
      $process.WaitForExit()
      throw "Packaged check timed out: $Arguments"
    }
    Write-Output $stdout.GetAwaiter().GetResult()
    Write-Output $stderr.GetAwaiter().GetResult()
    if ($process.ExitCode -ne $ExpectedExit) {
      throw "Packaged check exited $($process.ExitCode), expected ${ExpectedExit}: $Arguments"
    }
  } finally {
    $process.Dispose()
  }
}
