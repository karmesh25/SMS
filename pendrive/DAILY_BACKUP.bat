@echo off
setlocal EnableDelayedExpansion

set "DRIVE=%~d0"
set "ROOT=%DRIVE%\"
set "PGBIN=%ROOT%db\bin"
set "BACKUP=%ROOT%backup"
set "TIMESTAMP=%date:~-4%%date:4,2%%date:7,2%_%time:~0,2%%time:~3,2%%time:~6,2%"
set "TIMESTAMP=%TIMESTAMP: =0%"

if not exist "%BACKUP%" mkdir "%BACKUP%"

if not exist "%PGBIN%\pg_dump.exe" (
    echo ERROR: pg_dump not found at %PGBIN%
    exit /b 1
)

set "BACKUP_FILE=%BACKUP%\abr_backup_%TIMESTAMP%.sql"
echo Creating backup: %BACKUP_FILE%
"%PGBIN%\pg_dump.exe" -p 5433 -U postgres abr_db > "%BACKUP_FILE%"

if errorlevel 1 (
    echo ERROR: Backup failed.
    exit /b 1
)

echo Backup created successfully.

echo Cleaning backups older than the last 7 files...
for /f "skip=7 delims=" %%F in ('dir /b /o-d "%BACKUP%\abr_backup_*.sql" 2^>nul') do (
    del "%BACKUP%\%%F"
)

echo Done.
