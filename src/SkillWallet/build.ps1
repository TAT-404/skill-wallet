param([string]$OutputDirectory = (Join-Path $PSScriptRoot '..\SkillWallet-v0.6.4'))
$ErrorActionPreference = 'Stop'
$frameworkDir = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compilerPath = Join-Path $frameworkDir 'csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { throw '.NET Framework C# compiler not found.' }
$null = New-Item -ItemType Directory -Force -Path $OutputDirectory
$references = @('System.dll','System.Core.dll','System.Xaml.dll','System.Web.Extensions.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $frameworkDir $_) }
$sources = Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object { $_.FullName }
$exePath = Join-Path $OutputDirectory 'SkillWallet.exe'
$iconPath = Join-Path $PSScriptRoot 'wallet.ico'
if (-not (Test-Path -LiteralPath $iconPath)) { throw 'Missing wallet.ico brand asset.' }
& $compilerPath /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 "/win32icon:$iconPath" "/win32manifest:$PSScriptRoot\app.manifest" "/out:$exePath" "/resource:$PSScriptRoot\Assets\covers.png,SkillWallet.covers.png" "/resource:$PSScriptRoot\Assets\logo-mark.png,SkillWallet.logo-mark.png" "/resource:$PSScriptRoot\Assets\app-icon.png,SkillWallet.app-icon.png" "/resource:$PSScriptRoot\Assets\slide-tick.pcm,SkillWallet.slide-tick.pcm" @references @sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'SkillWallet.exe.config') -Destination $OutputDirectory -Force
Set-Content -LiteralPath (Join-Path $OutputDirectory 'portable.mode') -Value 'Skill Wallet portable mode. Data is stored in the adjacent data folder.' -Encoding UTF8
Write-Output $exePath
