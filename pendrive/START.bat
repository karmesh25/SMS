@echo off
setlocal EnableDelayedExpansion

set "DRIVE=%~d0"
set "ROOT=%DRIVE%\"
set "PGDATA=%ROOT%db\data"
set "PGBIN=%ROOT%db\bin"
set "APIDIR=%ROOT%api"
set "LOGDIR=%ROOT%logs"

if not exist "%LOGDIR%" mkdir "%LOGDIR%"

echo ========================================
echo  ABR Society Management System - START
echo ========================================
echo Drive: %DRIVE%
echo.

if not exist "%PGBIN%\pg_ctl.exe" (
    echo ERROR: Portable PostgreSQL not found at %PGBIN%
    echo Run SETUP_FIRST_RUN.bat first.
    pause
    exit /b 1
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
if exist "%APIDIR%\ABR.Api.exe" (
    start "" /D "%APIDIR%" "%APIDIR%\ABR.Api.exe"
) else (
    echo WARNING: Published API not found. Starting via dotnet for development...
    start "" cmd /c "cd /d %ROOT%backend\ABR.Api && dotnet run"
)

echo [4/4] Opening browser...
timeout /t 3 /nobreak >nul
start http://localhost:5050

echo.
echo ABR application started successfully.
echo Use STOP.bat to shut down.
pause
