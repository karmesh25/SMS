@echo off
setlocal EnableDelayedExpansion

set "DRIVE=%~d0"
set "ROOT=%DRIVE%\"
set "PGBIN=%ROOT%db\bin"
set "BACKUP=%ROOT%backup"
set "CONFIG=%ROOT%config"
set "TOOLS=%ROOT%tools"
set "SECRETS=%CONFIG%\secrets.enc"
set "SCRIPT_DIR=%~dp0"
set "TIMESTAMP=%date:~-4%%date:4,2%%date:7,2%_%time:~0,2%%time:~3,2%%time:~6,2%"
set "TIMESTAMP=%TIMESTAMP: =0%"

if not exist "%BACKUP%" mkdir "%BACKUP%"

if not exist "%PGBIN%\pg_dump.exe" (
    echo ERROR: pg_dump not found at %PGBIN%
    exit /b 1
)

if not exist "%SECRETS%" (
    echo ERROR: Encrypted secrets not found. Run SETUP_FIRST_RUN.bat first.
    exit /b 1
)

call "%SCRIPT_DIR%load_master_password.bat"
if errorlevel 1 exit /b 1

for /f "usebackq delims=" %%P in (`"%TOOLS%\ABR.Secrets.exe" dump-password --secrets "%SECRETS%"`) do set "PGPASSWORD=%%P"
if "!PGPASSWORD!"=="" (
    echo ERROR: Could not read database password from encrypted secrets.
    set "ABR_MASTER_PASSWORD="
    exit /b 1
)

set "TEMP_SQL=%BACKUP%\abr_backup_%TIMESTAMP%.sql.tmp"
set "BACKUP_FILE=%BACKUP%\abr_backup_%TIMESTAMP%.sql.enc"
echo Creating encrypted backup: %BACKUP_FILE%
"%PGBIN%\pg_dump.exe" -p 5433 -U postgres abr_db > "%TEMP_SQL%"
set "PGPASSWORD="

if errorlevel 1 (
    echo ERROR: Backup failed.
    if exist "%TEMP_SQL%" del /f /q "%TEMP_SQL%"
    set "ABR_MASTER_PASSWORD="
    exit /b 1
)

rem Encrypt the plaintext dump (AES-GCM, master password) then remove the plaintext.
"%TOOLS%\ABR.Secrets.exe" encrypt-file --in "%TEMP_SQL%" --out "%BACKUP_FILE%"
set "ENC_RESULT=!errorlevel!"
if exist "%TEMP_SQL%" del /f /q "%TEMP_SQL%"
set "ABR_MASTER_PASSWORD="

if not "!ENC_RESULT!"=="0" (
    echo ERROR: Failed to encrypt backup.
    if exist "%BACKUP_FILE%" del /f /q "%BACKUP_FILE%"
    exit /b 1
)

echo Encrypted backup created successfully.
echo Restore later with RESTORE_BACKUP.bat.

echo Cleaning backups older than the last 7 files...
for /f "skip=7 delims=" %%F in ('dir /b /o-d "%BACKUP%\abr_backup_*.sql.enc" 2^>nul') do (
    del "%BACKUP%\%%F"
)

echo Done.
