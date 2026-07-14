[CmdletBinding()]
param(
  [ValidateSet('copilot', 'claude', 'cursor')]
  [string]$Ai = 'copilot'
)

$ErrorActionPreference = 'Stop'
$src = Join-Path $PSScriptRoot 'skills\afk'
if (-not (Test-Path $src)) { throw "Skill source not found: $src" }

$dest = switch ($Ai) {
  'copilot' { Join-Path $HOME '.copilot\skills\afk' }
  'claude'  { Join-Path $HOME '.claude\skills\afk' }
  'cursor'  { Join-Path $HOME '.cursor\skills\afk' }
}

New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
Copy-Item $src $dest -Recurse -Force
Write-Host "Installed the afk skill to $dest"
Write-Host "Restart your agent so it picks up the new skill."

