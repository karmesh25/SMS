@echo off
setlocal EnableDelayedExpansion

set "DRIVE=%~d0"
set "ROOT=%DRIVE%\"
set "PGDATA=%ROOT%db\data"
set "PGBIN=%ROOT%db\bin"
set "CONFIG=%ROOT%config"
set "BACKUP=%ROOT%backup"
set "LOGDIR=%ROOT%logs"

echo ========================================
echo  ABR First-Time Setup
echo ========================================

if not exist "%CONFIG%" mkdir "%CONFIG%"
if not exist "%BACKUP%" mkdir "%BACKUP%"
if not exist "%LOGDIR%" mkdir "%LOGDIR%"

if not exist "%PGBIN%\initdb.exe" (
    echo ERROR: Portable PostgreSQL not found.
    echo Place PostgreSQL 15 portable binaries under %ROOT%db\bin\
    pause
    exit /b 1
)

if not exist "%PGDATA%" (
    echo [1/6] Initializing PostgreSQL data directory...
    "%PGBIN%\initdb.exe" -D "%PGDATA%" -U postgres -A trust -E UTF8
    if errorlevel 1 (
        echo ERROR: initdb failed.
        pause
        exit /b 1
    )
) else (
    echo [1/6] PostgreSQL data directory already exists - skipping initdb.
)

echo [2/6] Starting PostgreSQL...
"%PGBIN%\pg_ctl.exe" -D "%PGDATA%" -o "-p 5433" -l "%LOGDIR%\postgres-setup.log" start

echo [3/6] Creating database abr_db...
"%PGBIN%\createdb.exe" -p 5433 -U postgres abr_db 2>nul
if errorlevel 1 (
    echo Database may already exist - continuing.
)

echo [4/6] Applying EF Core migrations...
if exist "%ROOT%backend\ABR.Api\ABR.Api.csproj" (
    cd /d "%ROOT%backend"
    set "ConnectionStrings__DefaultConnection=Host=localhost;Port=5433;Database=abr_db;Username=postgres;Password=postgres"
    dotnet ef database update --project ABR.Infrastructure --startup-project ABR.Api
) else if exist "%ROOT%api\migrations.sql" (
    "%PGBIN%\psql.exe" -p 5433 -U postgres -d abr_db -f "%ROOT%api\migrations.sql"
) else (
    echo WARNING: No migration source found. Run dotnet ef database update manually.
)

echo [5/6] Creating device license placeholder...
echo PLACEHOLDER_DEVICE_LICENSE > "%CONFIG%\device.lic"
echo Device license placeholder created. Phase 1 will collect hardware fingerprint.

echo [6/6] Setup complete.
echo.
echo Default admin credentials (seeded by DbInitializer):
echo   Username: admin
echo   Password: Admin@123
echo.
echo Change the admin password after first login in Phase 1.
pause
