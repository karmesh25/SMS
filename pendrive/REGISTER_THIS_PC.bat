@echo off
setlocal EnableDelayedExpansion

set "DRIVE=%~d0"
set "ROOT=%DRIVE%\"
set "PGDATA=%ROOT%db\data"
set "PGBIN=%ROOT%db\bin"

echo ========================================
echo  ABR Register This PC (One-Time)
echo ========================================
echo.
echo This starts the app with device lock temporarily OFF
echo so you can authorize this computer.
echo.
echo Steps after the browser opens:
echo   1. Login: admin / Admin@123
echo   2. Go to Admin -^> Devices
echo   3. Click Authorize This PC
echo   4. Run STOP.bat when finished
echo   5. Use START.bat for normal daily use
echo.

if not exist "%PGDATA%" (
    echo ERROR: Database not initialized. Run SETUP_FIRST_RUN.bat first.
    pause
    exit /b 1
)

if not exist "%PGBIN%\pg_ctl.exe" (
    echo ERROR: Portable PostgreSQL not found at %PGBIN%
    pause
    exit /b 1
)

set "Security__EnforceDeviceLock=false"
call "%~dp0START.bat"

endlocal
