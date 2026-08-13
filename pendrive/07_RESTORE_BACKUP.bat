@echo off
setlocal EnableDelayedExpansion

set "DRIVE=%~d0"
set "ROOT=%DRIVE%\"
set "PGDATA=%ROOT%db\data"
set "PGBIN=%ROOT%db\bin"
set "BACKUP=%ROOT%backup"
set "CONFIG=%ROOT%config"
set "TOOLS=%ROOT%tools"
set "LOGDIR=%ROOT%logs"
set "SECRETS=%CONFIG%\secrets.enc"
set "SCRIPT_DIR=%~dp0"

echo ========================================
echo  ABR Restore Encrypted Backup
echo ========================================
echo.
echo WARNING: This REPLACES all current data in abr_db with the backup.
echo.

if not exist "%PGBIN%\psql.exe" (
    echo ERROR: PostgreSQL not found at %PGBIN%
    pause
    exit /b 1
)
if not exist "%SECRETS%" (
    echo ERROR: Encrypted secrets not found. Run SETUP_FIRST_RUN.bat first.
    pause
    exit /b 1
)
if not exist "%TOOLS%\ABR.Secrets.exe" (
    echo ERROR: ABR.Secrets.exe not found at %TOOLS%
    pause
    exit /b 1
)

rem Choose the backup file: first argument, or the most recent *.sql.enc.
set "BACKUP_FILE=%~1"
if "%BACKUP_FILE%"=="" (
    for /f "delims=" %%F in ('dir /b /o-d "%BACKUP%\abr_backup_*.sql.enc" 2^>nul') do (
        set "BACKUP_FILE=%BACKUP%\%%F"
        goto :gotfile
    )
)
:gotfile
if "%BACKUP_FILE%"=="" (
    echo ERROR: No encrypted backup found in %BACKUP%
    pause
    exit /b 1
)

echo Backup to restore: %BACKUP_FILE%
echo.
set /p CONFIRM="Type YES to overwrite abr_db with this backup: "
if /I not "!CONFIRM!"=="YES" (
    echo Cancelled.
    pause
    exit /b 0
)

call "%SCRIPT_DIR%load_master_password.bat"
if errorlevel 1 exit /b 1

rem Verify the master password before doing anything destructive.
"%TOOLS%\ABR.Secrets.exe" verify --secrets "%SECRETS%"
if errorlevel 1 (
    echo ERROR: Invalid master password.
    set "ABR_MASTER_PASSWORD="
    pause
    exit /b 1
)

for /f "usebackq delims=" %%P in (`"%TOOLS%\ABR.Secrets.exe" dump-password --secrets "%SECRETS%"`) do set "PGPASSWORD=%%P"

echo Stopping ABR API to release database connections...
taskkill /IM ABR.Api.exe /F >nul 2>&1

echo Ensuring PostgreSQL is running...
"%PGBIN%\pg_ctl.exe" -D "%PGDATA%" -o "-p 5433" -l "%LOGDIR%\postgres.log" start >nul 2>&1
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
    set "PGPASSWORD="
    set "ABR_MASTER_PASSWORD="
    pause
    exit /b 1
)

set "TEMP_SQL=%BACKUP%\_restore_tmp.sql"
echo Decrypting backup...
"%TOOLS%\ABR.Secrets.exe" decrypt-file --in "%BACKUP_FILE%" --out "%TEMP_SQL%"
if errorlevel 1 (
    echo ERROR: Failed to decrypt backup (wrong master password or corrupted file).
    set "PGPASSWORD="
    set "ABR_MASTER_PASSWORD="
    pause
    exit /b 1
)
set "ABR_MASTER_PASSWORD="

echo Recreating database abr_db...
"%PGBIN%\psql.exe" -p 5433 -U postgres -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='abr_db' AND pid <> pg_backend_pid();" >nul 2>&1
"%PGBIN%\psql.exe" -p 5433 -U postgres -d postgres -c "DROP DATABASE IF EXISTS abr_db;"
if errorlevel 1 (
    echo ERROR: Could not drop abr_db.
    goto :cleanup_fail
)
"%PGBIN%\psql.exe" -p 5433 -U postgres -d postgres -c "CREATE DATABASE abr_db;"
if errorlevel 1 (
    echo ERROR: Could not create abr_db.
    goto :cleanup_fail
)

echo Restoring data...
"%PGBIN%\psql.exe" -p 5433 -U postgres -d abr_db -f "%TEMP_SQL%"
if errorlevel 1 (
    echo ERROR: Restore failed.
    goto :cleanup_fail
)

if exist "%TEMP_SQL%" del /f /q "%TEMP_SQL%"
set "PGPASSWORD="
echo.
echo Restore complete. Start the app with START.bat.
pause
exit /b 0

:cleanup_fail
if exist "%TEMP_SQL%" del /f /q "%TEMP_SQL%"
set "PGPASSWORD="
set "ABR_MASTER_PASSWORD="
pause
exit /b 1
