@echo off
set "MASTER_PASSWORD=%~1"
if "%MASTER_PASSWORD%"=="" (
    for /f "usebackq delims=" %%P in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0prompt_master_password.ps1"`) do set "MASTER_PASSWORD=%%P"
)
if "%MASTER_PASSWORD%"=="" (
    echo ERROR: Master password is required.
    exit /b 1
)
set "ABR_MASTER_PASSWORD=%MASTER_PASSWORD%"
exit /b 0
