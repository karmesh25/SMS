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
taskkill /FI "WINDOWTITLE eq ABR.Api*" /F >nul 2>&1

if exist "%PGBIN%\pg_ctl.exe" (
    echo Stopping PostgreSQL...
    "%PGBIN%\pg_ctl.exe" -D "%PGDATA%" stop -m fast
) else (
    echo PostgreSQL binaries not found - skipping DB shutdown.
)

echo Shutdown complete.
pause
