$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$artifacts = Join-Path $repoRoot "artifacts"
$bin = Join-Path $artifacts "bin"
$dist = Join-Path $artifacts "dist"
$packageRoot = Join-Path $dist "GitHubUploader"
$zipPath = Join-Path $dist "GitHubUploader.zip"

function Assert-UnderRepo {
  param([string]$Path)
  $fullPath = [System.IO.Path]::GetFullPath($Path)
  $fullRoot = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd("\") + "\"
  if (-not $fullPath.StartsWith($fullRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to modify path outside repository: $fullPath"
  }
}

& (Join-Path $repoRoot "scripts\build.ps1") -OutputDir $bin

Assert-UnderRepo $packageRoot
Assert-UnderRepo $zipPath

if (Test-Path $packageRoot) {
  Remove-Item -LiteralPath $packageRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $bin "GitHubUploader.exe") -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "README.md") -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "LICENSE") -Destination $packageRoot -Force

if (Test-Path $zipPath) {
  Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $zipPath
Write-Host "Packaged: $zipPath"
