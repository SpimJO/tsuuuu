@echo off
cd /d "%~dp0"
if not exist "%~dp0.venv\Scripts\label-studio.exe" (
  echo Run SETUP.bat first.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start_all.ps1"
