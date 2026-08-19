# Start image server. Keep this window open.
# Port 9091 — does not collide with tsuorg-ml (9090).

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
Set-Location $Root

$py = Join-Path $Root ".venv\Scripts\python.exe"
if (-not (Test-Path $py)) {
    Write-Host "Run SETUP.bat first." -ForegroundColor Red
    exit 1
}

$images = Join-Path $Root "data\raw\images"
if (-not (Test-Path (Join-Path $images "sf08"))) {
    Write-Host "Missing data\raw\images\sf08. This copy is incomplete." -ForegroundColor Red
    exit 1
}

Write-Host "=== SF08 image server ===" -ForegroundColor Cyan
Write-Host "http://127.0.0.1:9091"
Write-Host "Keep this window open."
Write-Host ""

& $py -m training.scripts.serve_label_images --port 9091
