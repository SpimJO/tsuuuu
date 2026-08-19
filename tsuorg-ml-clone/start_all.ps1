# Opens image server + Label Studio in two windows.
# Double-click START.bat after SETUP.bat.

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
Set-Location $Root

$venvPy = Join-Path $Root ".venv\Scripts\python.exe"
$lsExe = Join-Path $Root ".venv\Scripts\label-studio.exe"
if (-not (Test-Path $venvPy) -or -not (Test-Path $lsExe)) {
    Write-Host "Run SETUP.bat first (.\setup_labeling.ps1)." -ForegroundColor Red
    exit 1
}

$bypass = "-NoExit -ExecutionPolicy Bypass -File"
Start-Process powershell.exe -ArgumentList "$bypass `"$Root\start_labeling.ps1`""
Start-Sleep -Seconds 2
Start-Process powershell.exe -ArgumentList "$bypass `"$Root\start_label_studio.ps1`""

Write-Host "Started two windows:" -ForegroundColor Green
Write-Host "  1) Image server  http://127.0.0.1:9091"
Write-Host "  2) Label Studio  http://localhost:8081"
Write-Host "Keep both open while labeling."
Start-Sleep -Seconds 8
Start-Process "http://localhost:8081"
