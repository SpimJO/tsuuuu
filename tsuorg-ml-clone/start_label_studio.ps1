# Start Label Studio for this clone only (port 8081, local data folder).

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
Set-Location $Root

$lsExe = Join-Path $Root ".venv\Scripts\label-studio.exe"
if (-not (Test-Path $lsExe)) {
    Write-Host "Run SETUP.bat first." -ForegroundColor Red
    exit 1
}

$absImages = Join-Path $Root "data\raw\images"
$lsDataDir = Join-Path $Root "label-studio-data"
New-Item -ItemType Directory -Force -Path $lsDataDir | Out-Null

$env:LABEL_STUDIO_BASE_DATA_DIR = $lsDataDir
$env:LABEL_STUDIO_LOCAL_FILES_SERVING_ENABLED = "true"
$env:LABEL_STUDIO_LOCAL_FILES_DOCUMENT_ROOT = $absImages

@"
LABEL_STUDIO_LOCAL_FILES_SERVING_ENABLED=true
LABEL_STUDIO_LOCAL_FILES_DOCUMENT_ROOT=$absImages
"@ | Set-Content -Path (Join-Path $lsDataDir ".env") -Encoding UTF8

Write-Host "=== Label Studio ===" -ForegroundColor Cyan
Write-Host "Open:  http://localhost:8081"
Write-Host ""
Write-Host "First time:" -ForegroundColor Yellow
Write-Host "  1. Sign up (email can be anything local, e.g. you@local)"
Write-Host "  2. Create project"
Write-Host "  3. Labeling Interface -> Code -> paste data\templates\label_studio_config.xml"
Write-Host "  4. Import data\raw\label_studio_sf08_import.json"
Write-Host "  5. Image server window must stay running"
Write-Host ""

& $lsExe start -p 8081
