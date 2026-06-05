$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$exe = Join-Path $repoRoot "artifacts\bin\GitHubOneClickUploader.exe"

& (Join-Path $repoRoot "scripts\build.ps1")

$process = Start-Process -FilePath $exe -ArgumentList "--self-test" -Wait -PassThru
if ($process.ExitCode -ne 0) {
  throw "Self-test failed with exit code $($process.ExitCode)."
}

Write-Host "Self-test passed."
