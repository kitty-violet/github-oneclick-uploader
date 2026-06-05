$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$artifacts = Join-Path $repoRoot "artifacts"
$bin = Join-Path $artifacts "bin"
$dist = Join-Path $artifacts "dist"
$packageRoot = Join-Path $dist "GitHubOneClickUploader"
$zipPath = Join-Path $dist "GitHubOneClickUploader.zip"
$shaPath = Join-Path $dist "GitHubOneClickUploader.zip.sha256"

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
Assert-UnderRepo $shaPath

if (Test-Path $dist) {
  Get-ChildItem -LiteralPath $dist -Force | Remove-Item -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $bin "GitHubOneClickUploader.exe") -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "README.md") -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "LICENSE") -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "CHANGELOG.md") -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "SECURITY.md") -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "docs") -Destination (Join-Path $packageRoot "docs") -Recurse -Force

Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $zipPath
$hash = Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath
($hash.Hash.ToLowerInvariant() + "  GitHubOneClickUploader.zip") | Set-Content -LiteralPath $shaPath -Encoding ASCII
Remove-Item -LiteralPath $packageRoot -Recurse -Force
Write-Host "Packaged: $zipPath"
Write-Host "SHA256: $shaPath"
