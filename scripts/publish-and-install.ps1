$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$installDir = "C:\soft\pgy"
$publishDir = Join-Path $root "publish"
dotnet publish (Join-Path $root "src\PgyerApkUploader\PgyerApkUploader.csproj") `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $publishDir -p:AssemblyName=pgy
if (-not (Test-Path $installDir)) { New-Item -ItemType Directory -Path $installDir | Out-Null }
Copy-Item -Force (Join-Path $publishDir "pgy.exe") (Join-Path $installDir "pgy.exe")
Write-Host "Installed: $(Join-Path $installDir 'pgy.exe') @ $((Get-Item (Join-Path $installDir 'pgy.exe')).LastWriteTime)"
