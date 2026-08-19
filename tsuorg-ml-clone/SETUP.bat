@echo off
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup_labeling.ps1"
if errorlevel 1 pause
exit /b %errorlevel%
