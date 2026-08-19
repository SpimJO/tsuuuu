# First-time setup. Double-click SETUP.bat, or:
#   powershell -ExecutionPolicy Bypass -File .\setup_labeling.ps1

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
Set-Location $Root

Write-Host "=== TSU-ORGDOCX labeling setup ===" -ForegroundColor Cyan
Write-Host "Folder: $Root"
Write-Host ""

function Get-PythonLauncher {
    if (Get-Command py -ErrorAction SilentlyContinue) {
        & py -3 --version 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { return @{ Exe = "py"; Args = @("-3") } }
    }
    if (Get-Command python -ErrorAction SilentlyContinue) {
        return @{ Exe = "python"; Args = @() }
    }
    throw @"
Python 3.10+ was not found on PATH.

Install from https://www.python.org/downloads/
On the installer, check: Add python.exe to PATH
Then run SETUP.bat again.
"@
}

$images = Join-Path $Root "data\raw\images\sf08"
$import = Join-Path $Root "data\raw\label_studio_sf08_import.json"
$xml = Join-Path $Root "data\templates\label_studio_config.xml"

if (-not (Test-Path $images)) {
    throw "Missing SF08 images at data\raw\images\sf08. Zip/send the whole tsuorg-ml-clone folder, including data\."
}
$pngCount = (Get-ChildItem $images -Filter *.png -File).Count
if ($pngCount -lt 1) {
    throw "No PNG files in data\raw\images\sf08. The image pack is missing from this copy."
}
if (-not (Test-Path $import)) { throw "Missing $import" }
if (-not (Test-Path $xml)) { throw "Missing $xml" }

Write-Host "Found $pngCount SF08 page images." -ForegroundColor Green

# Keep import URLs on this clone's image port (9091).
$raw = [System.IO.File]::ReadAllText($import)
$fixed = $raw -replace "http://127\.0\.0\.1:9090", "http://127.0.0.1:9091"
if ($fixed -ne $raw) {
    [System.IO.File]::WriteAllText($import, $fixed)
    Write-Host "Updated import JSON to http://127.0.0.1:9091" -ForegroundColor Yellow
}

$py = Get-PythonLauncher
Write-Host "Python: $($py.Exe) $($py.Args -join ' ')"
& $py.Exe @($py.Args + @("--version"))

$venvPy = Join-Path $Root ".venv\Scripts\python.exe"
if (-not (Test-Path $venvPy)) {
    Write-Host "Creating virtualenv..." -ForegroundColor Yellow
    & $py.Exe @($py.Args + @("-m", "venv", ".venv"))
}

Write-Host "Installing Label Studio (first time can take several minutes)..." -ForegroundColor Yellow
& $venvPy -m pip install --upgrade pip
& $venvPy -m pip install -r (Join-Path $Root "requirements-labeling.txt")

Write-Host ""
Write-Host "Setup OK." -ForegroundColor Green
Write-Host "Next: double-click START.bat  (or run .\start_all.ps1)"
Write-Host "Then open http://localhost:8081"
