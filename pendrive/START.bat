@echo off
setlocal EnableDelayedExpansion

set "DRIVE=%~d0"
set "ROOT=%DRIVE%\"
set "PGDATA=%ROOT%db\data"
set "PGBIN=%ROOT%db\bin"
set "APIDIR=%ROOT%api"
set "LOGDIR=%ROOT%logs"
set "CONFIG=%ROOT%config"
set "EXPORTS=%ROOT%exports"
set "TOOLS=%ROOT%tools"
set "SECRETS=%CONFIG%\secrets.enc"
set "SCRIPT_DIR=%~dp0"

if not exist "%LOGDIR%" mkdir "%LOGDIR%"
if not exist "%CONFIG%" mkdir "%CONFIG%"
if not exist "%EXPORTS%" mkdir "%EXPORTS%"

echo ========================================
echo  ABR Society Management System - START
echo ========================================
echo Drive: %DRIVE%
echo.

if not exist "%PGBIN%\pg_ctl.exe" (
    echo ERROR: Portable PostgreSQL not found at %PGBIN%
    echo See db\README_POSTGRES.txt for setup instructions.
    pause
    exit /b 1
)

if not exist "%PGDATA%" (
    echo ERROR: Database not initialized.
    echo Run SETUP_FIRST_RUN.bat first.
    pause
    exit /b 1
)

if exist "%SECRETS%" (
    if not exist "%TOOLS%\ABR.Secrets.exe" (
        echo ERROR: ABR.Secrets.exe not found at %TOOLS%
        pause
        exit /b 1
    )
    call "%SCRIPT_DIR%load_master_password.bat"
    if errorlevel 1 exit /b 1
    "%TOOLS%\ABR.Secrets.exe" verify --master-password "!ABR_MASTER_PASSWORD!" --secrets "%SECRETS%"
    if errorlevel 1 (
        echo ERROR: Invalid master password.
        set "ABR_MASTER_PASSWORD="
        pause
        exit /b 1
    )
)

echo [1/4] Starting PostgreSQL on port 5433...
"%PGBIN%\pg_ctl.exe" -D "%PGDATA%" -o "-p 5433" -l "%LOGDIR%\postgres.log" start
if errorlevel 1 (
    echo ERROR: Failed to start PostgreSQL.
    pause
    exit /b 1
)

echo [2/4] Waiting for PostgreSQL...
set /a RETRIES=0
:WAITPG
set /a RETRIES+=1
"%PGBIN%\pg_isready.exe" -p 5433 >nul 2>&1
if errorlevel 1 (
    if !RETRIES! LSS 30 (
        timeout /t 1 /nobreak >nul
        goto WAITPG
    )
    echo ERROR: PostgreSQL did not become ready in time.
    pause
    exit /b 1
)

echo [3/4] Starting ABR API on port 5050...
if not defined ASPNETCORE_ENVIRONMENT set "ASPNETCORE_ENVIRONMENT=Production"

if exist "%APIDIR%\ABR.Api.exe" (
    start "" /D "%APIDIR%" "%APIDIR%\ABR.Api.exe"
) else (
    echo ERROR: Published API not found at %APIDIR%\ABR.Api.exe
    echo Run pendrive\build_pendrive.bat on your development machine first.
    pause
    exit /b 1
)

echo [4/4] Opening browser...
timeout /t 5 /nobreak >nul
start http://localhost:5050

echo.
echo ABR application started successfully.
echo   URL: http://localhost:5050
echo   Reports save to: %EXPORTS%
echo.
if /I "%Security__EnforceDeviceLock%"=="false" (
    echo Device lock is OFF for registration. Authorize this PC in Admin -^> Devices.
    echo Then run STOP.bat and use START.bat normally.
) else (
    echo Use STOP.bat before removing the USB drive.
)
echo.
set "ABR_MASTER_PASSWORD="
pause
