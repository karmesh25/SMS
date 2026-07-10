@echo off
setlocal EnableDelayedExpansion

set "DRIVE=%~d0"
set "ROOT=%DRIVE%\"
set "PGDATA=%ROOT%db\data"
set "PGBIN=%ROOT%db\bin"
set "CONFIG=%ROOT%config"
set "TOOLS=%ROOT%tools"
set "SECRETS=%CONFIG%\secrets.enc"
set "SETUP_PWD=%CONFIG%\.setup_db_password.tmp"
set "SCRIPT_DIR=%~dp0"

echo ========================================
echo  ABR Security Upgrade
echo ========================================
echo.

if not exist "%PGDATA%" (
    echo ERROR: Run SETUP_FIRST_RUN.bat on a new USB instead.
    pause
    exit /b 1
)

if exist "%SECRETS%" (
    echo secrets.enc already exists. Security upgrade not required.
    pause
    exit /b 0
)

if not exist "%TOOLS%\ABR.Secrets.exe" (
    echo ERROR: ABR.Secrets.exe not found at %TOOLS%
    pause
    exit /b 1
)

echo Choose a NEW master password for encrypted secrets...
for /f "usebackq delims=" %%P in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%prompt_setup_master_password.ps1"`) do set "MASTER_PASSWORD=%%P"
if "!MASTER_PASSWORD!"=="" (
    echo ERROR: Master password setup cancelled.
    pause
    exit /b 1
)

echo Starting PostgreSQL if needed...
"%PGBIN%\pg_ctl.exe" -D "%PGDATA%" -o "-p 5433" -l "%ROOT%logs\postgres-upgrade.log" start >nul 2>&1
timeout /t 2 /nobreak >nul

echo Generating encrypted secrets...
set "ABR_MASTER_PASSWORD=!MASTER_PASSWORD!"
"%TOOLS%\ABR.Secrets.exe" generate --output "%SECRETS%" --setup-password-file "%SETUP_PWD%"
if errorlevel 1 (
    echo ERROR: Failed to generate secrets.enc
    if exist "%SETUP_PWD%" del /f /q "%SETUP_PWD%"
    pause
    exit /b 1
)

set "DB_PASSWORD="
for /f "usebackq delims=" %%P in ("%SETUP_PWD%") do set "DB_PASSWORD=%%P"

echo Setting PostgreSQL password...
"%PGBIN%\psql.exe" -p 5433 -U postgres -d postgres -c "ALTER USER postgres WITH PASSWORD '!DB_PASSWORD!';"
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%harden_pg_hba.ps1" -PgData "%PGDATA%"
if errorlevel 1 (
    echo ERROR: Failed to enforce scram-sha-256 in pg_hba.conf.
    if exist "%SETUP_PWD%" del /f /q "%SETUP_PWD%"
    pause
    exit /b 1
)
"%PGBIN%\pg_ctl.exe" -D "%PGDATA%" reload

if exist "%SETUP_PWD%" del /f /q "%SETUP_PWD%"
set "MASTER_PASSWORD="
set "DB_PASSWORD="
set "ABR_MASTER_PASSWORD="

echo.
echo Security upgrade complete.
echo Use START.bat and enter your master password when prompted.
echo.
pause
