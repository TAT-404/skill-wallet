param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist\SkillWallet'))
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'src\SkillWallet\build.ps1') -OutputDirectory $OutputDirectory
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs\使用说明.md') -Destination $OutputDirectory -Force
