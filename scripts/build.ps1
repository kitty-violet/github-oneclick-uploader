param(
  [string]$OutputDir
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$source = Join-Path $repoRoot "src\GitHubUploader\GitHubUploader.cs"

if (-not $OutputDir) {
  $OutputDir = Join-Path $repoRoot "artifacts\bin"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$candidates = @(
  (Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
  (Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe")
)

$csc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
  throw "Could not find csc.exe under $env:WINDIR\Microsoft.NET\Framework*."
}

$out = Join-Path $OutputDir "GitHubUploader.exe"

& $csc `
  /nologo `
  /target:winexe `
  /platform:x64 `
  /optimize+ `
  /warn:4 `
  /out:$out `
  /r:System.dll `
  /r:System.Core.dll `
  /r:System.Windows.Forms.dll `
  /r:System.Drawing.dll `
  $source

Write-Host "Built: $out"
