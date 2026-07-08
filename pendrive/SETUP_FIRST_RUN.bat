@echo off
setlocal EnableDelayedExpansion

set "DRIVE=%~d0"
set "ROOT=%DRIVE%\"
set "PGDATA=%ROOT%db\data"
set "PGBIN=%ROOT%db\bin"
set "CONFIG=%ROOT%config"
set "TOOLS=%ROOT%tools"
set "BACKUP=%ROOT%backup"
set "LOGDIR=%ROOT%logs"
set "EXPORTS=%ROOT%exports"
set "SECRETS=%CONFIG%\secrets.enc"
set "SETUP_PWD=%CONFIG%\.setup_db_password.tmp"
set "SCRIPT_DIR=%~dp0"

echo ========================================
echo  ABR First-Time Setup
echo ========================================
echo Drive: %DRIVE%
echo.

if not exist "%CONFIG%" mkdir "%CONFIG%"
if not exist "%BACKUP%" mkdir "%BACKUP%"
if not exist "%LOGDIR%" mkdir "%LOGDIR%"
if not exist "%EXPORTS%" mkdir "%EXPORTS%"

if not exist "%PGBIN%\initdb.exe" (
    echo ERROR: Portable PostgreSQL not found.
    echo Place PostgreSQL 15 portable binaries under %ROOT%db\bin\
    echo See %ROOT%db\README_POSTGRES.txt
    pause
    exit /b 1
)

if not exist "%TOOLS%\ABR.Secrets.exe" (
    echo ERROR: ABR.Secrets.exe not found at %TOOLS%
    echo Rebuild the pendrive package on your development machine.
    pause
    exit /b 1
)

if exist "%SECRETS%" (
    echo ERROR: secrets.enc already exists. Setup was already completed.
    echo To upgrade an older USB, run UPGRADE_SECURITY.bat instead.
    pause
    exit /b 1
)

echo [1/8] Choose master password for encrypted secrets...
for /f "usebackq delims=" %%P in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%prompt_setup_master_password.ps1"`) do set "MASTER_PASSWORD=%%P"
if "!MASTER_PASSWORD!"=="" (
    echo ERROR: Master password setup cancelled.
    pause
    exit /b 1
)

if not exist "%PGDATA%" (
    echo [2/8] Initializing PostgreSQL data directory...
    "%PGBIN%\initdb.exe" -D "%PGDATA%" -U postgres -A trust -E UTF8
    if errorlevel 1 (
        echo ERROR: initdb failed.
        pause
        exit /b 1
    )
) else (
    echo [2/8] PostgreSQL data directory already exists - skipping initdb.
)

echo [3/8] Starting PostgreSQL...
"%PGBIN%\pg_ctl.exe" -D "%PGDATA%" -o "-p 5433" -l "%LOGDIR%\postgres-setup.log" start
if errorlevel 1 (
    echo ERROR: Failed to start PostgreSQL.
    pause
    exit /b 1
)

echo [4/8] Waiting for PostgreSQL...
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

echo [5/8] Generating encrypted secrets and database password...
"%TOOLS%\ABR.Secrets.exe" generate --master-password "!MASTER_PASSWORD!" --output "%SECRETS%" --setup-password-file "%SETUP_PWD%"
if errorlevel 1 (
    echo ERROR: Failed to generate secrets.enc
    if exist "%SETUP_PWD%" del /f /q "%SETUP_PWD%"
    pause
    exit /b 1
)

set "DB_PASSWORD="
for /f "usebackq delims=" %%P in ("%SETUP_PWD%") do set "DB_PASSWORD=%%P"
if "!DB_PASSWORD!"=="" (
    echo ERROR: Database password was not generated.
    if exist "%SETUP_PWD%" del /f /q "%SETUP_PWD%"
    pause
    exit /b 1
)

echo [6/8] Setting strong PostgreSQL password...
"%PGBIN%\psql.exe" -p 5433 -U postgres -d postgres -c "ALTER USER postgres WITH PASSWORD '!DB_PASSWORD!';"
if errorlevel 1 (
    echo ERROR: Failed to set PostgreSQL password.
    if exist "%SETUP_PWD%" del /f /q "%SETUP_PWD%"
    pause
    exit /b 1
)

echo [7/8] Enforcing scram-sha-256 authentication...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$path = '%PGDATA%\pg_hba.conf';" ^
  "$content = Get-Content $path;" ^
  "$content = $content | ForEach-Object { $_ -replace '\ttrust$', \"`tscram-sha-256\" };" ^
  "Set-Content -Path $path -Value $content"
"%PGBIN%\pg_ctl.exe" -D "%PGDATA%" reload

set "PGPASSWORD=!DB_PASSWORD!"
echo [8/8] Creating database abr_db...
"%PGBIN%\createdb.exe" -p 5433 -U postgres abr_db 2>nul
if errorlevel 1 (
    echo Database may already exist - continuing.
)
set "PGPASSWORD="
if exist "%SETUP_PWD%" del /f /q "%SETUP_PWD%"
set "MASTER_PASSWORD="

echo.
echo Setup complete.
echo   - Encrypted secrets: %SECRETS%
echo   - PostgreSQL requires password (no trust auth)
echo   - Database tables are created on first START
echo.
echo NEXT STEPS:
echo   1. Run REGISTER_THIS_PC.bat (one-time device authorization)
echo   2. Run STOP.bat, then START.bat (enter master password when prompted)
echo.
echo IMPORTANT: If you forget the master password, secrets cannot be recovered.
echo Default app login after first API start: admin / Admin@123
echo.
pause
