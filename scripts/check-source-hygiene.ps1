$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$ignoredDirs = @(".git", "artifacts", "bin", "obj", ".vs")
$blockedNames = @(".env", ".env.local", "id_rsa", "id_dsa", "credentials.json", "token.json")
$patterns = @(
  "C:\\Users\\[A-Za-z0-9._-]+",
  ("E:" + "\\codex-tools"),
  ("D:" + "\\git"),
  ("Len" + "ovo"),
  ("kitty" + "-violet"),
  ("209" + "858054"),
  "AIza[0-9A-Za-z_-]{20,}",
  "AQ\.[A-Za-z0-9_-]{20,}",
  "sk-[A-Za-z0-9_-]{20,}",
  "github_pat_[0-9A-Za-z_]{20,}",
  "gh[pousr]_[0-9A-Za-z_]{20,}"
)

$failures = New-Object System.Collections.Generic.List[string]

Get-ChildItem -LiteralPath $repoRoot -Recurse -Force -File | ForEach-Object {
  $file = $_
  $relative = $file.FullName.Substring($repoRoot.Length).TrimStart("\")
  $parts = $relative -split "[\\/]"
  if ($parts | Where-Object { $ignoredDirs -contains $_ }) { return }
  if ($blockedNames -contains $file.Name) {
    $failures.Add("Blocked file: $relative")
    return
  }
  if (@(".exe", ".dll", ".png", ".jpg", ".jpeg", ".ico", ".zip") -contains $file.Extension.ToLowerInvariant()) { return }
  $text = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction SilentlyContinue
  foreach ($pattern in $patterns) {
    if ($text -match $pattern) {
      $failures.Add("Pattern '$pattern' matched in $relative")
    }
  }
}

if ($failures.Count -gt 0) {
  $failures | ForEach-Object { Write-Host $_ }
  throw "Source hygiene check failed."
}

Write-Host "Source hygiene check passed."
