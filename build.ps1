# Builds the app, runs the tests and, if Inno Setup is installed, the installer.
# Usage: powershell -ExecutionPolicy Bypass -File build.ps1
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

[xml]$proj = Get-Content "src\TarekGamingSpace\TarekGamingSpace.csproj"
$version = $proj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

dotnet test -c Release
if ($LASTEXITCODE -ne 0) { throw "Tests failed" }

if (Test-Path artifacts\app) { Remove-Item artifacts\app -Recurse -Force }
dotnet publish src\TarekGamingSpace -c Release -o artifacts\app
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

$iscc = @(
    (Get-Command iscc -ErrorAction SilentlyContinue).Source,
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

if (-not $iscc) {
    Write-Host "App published to artifacts\app. Install Inno Setup to build the installer: winget install JRSoftware.InnoSetup"
    exit 0
}
& $iscc "/DAppVersion=$version" installer\TarekGamingSpace.iss
if ($LASTEXITCODE -ne 0) { throw "Installer build failed" }
Write-Host "Installer: installer\Output\TarekGamingSpace-Setup-$version.exe"
