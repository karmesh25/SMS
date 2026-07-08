@echo off
setlocal

set "DRIVE=%~d0"
set "ROOT=%DRIVE%\"
set "PGDATA=%ROOT%db\data"
set "PGBIN=%ROOT%db\bin"

echo ========================================
echo  ABR Society Management System - STOP
echo ========================================

echo Stopping ABR API...
taskkill /IM ABR.Api.exe /F >nul 2>&1

if exist "%PGBIN%\pg_ctl.exe" (
    if exist "%PGDATA%" (
        echo Stopping PostgreSQL...
        "%PGBIN%\pg_ctl.exe" -D "%PGDATA%" stop -m fast >nul 2>&1
    )
) else (
    echo PostgreSQL binaries not found - skipping DB shutdown.
)

echo.
echo Shutdown complete. You may safely remove the USB drive.
pause
