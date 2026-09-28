@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SKIP_POSTGRES=0"
set "PACKAGE_ROOT=%~dp0"

if /I "%~1"=="skip-postgres" (
    set "SKIP_POSTGRES=1"
    if not "%~2"=="" set "PACKAGE_ROOT=%~2"
) else if not "%~1"=="" (
    set "PACKAGE_ROOT=%~1"
)

if not "%PACKAGE_ROOT:~-1%"=="\" set "PACKAGE_ROOT=%PACKAGE_ROOT%\"

echo ========================================
echo  ABR Package Validation
echo ========================================
echo Checking: %PACKAGE_ROOT%
echo.

set "ERRORS=0"

call :check_file "%PACKAGE_ROOT%api\ABR.Api.exe" "API executable"
call :check_file "%PACKAGE_ROOT%api\wwwroot\index.html" "Angular UI - wwwroot/index.html"
call :check_file "%PACKAGE_ROOT%api\appsettings.json" "API appsettings"
call :check_file "%PACKAGE_ROOT%02_SETUP_FIRST_RUN.bat" "02_SETUP_FIRST_RUN.bat"
call :check_file "%PACKAGE_ROOT%03_REGISTER_THIS_PC.bat" "03_REGISTER_THIS_PC.bat"
call :check_file "%PACKAGE_ROOT%04_START.bat" "04_START.bat"
call :check_file "%PACKAGE_ROOT%05_STOP.bat" "05_STOP.bat"
call :check_file "%PACKAGE_ROOT%06_DAILY_BACKUP.bat" "06_DAILY_BACKUP.bat"
call :check_file "%PACKAGE_ROOT%07_RESTORE_BACKUP.bat" "07_RESTORE_BACKUP.bat"
call :check_file "%PACKAGE_ROOT%harden_pg_hba.ps1" "pg_hba hardening script"
call :check_file "%PACKAGE_ROOT%CLIENT_SETUP_AND_START.txt" "Client setup guide"
call :check_file "%PACKAGE_ROOT%tools\ABR.Secrets.exe" "Secrets tool"

if "%SKIP_POSTGRES%"=="0" (
    call :check_file "%PACKAGE_ROOT%db\bin\pg_ctl.exe" "PostgreSQL portable - pg_ctl.exe"
    call :check_file "%PACKAGE_ROOT%db\bin\initdb.exe" "PostgreSQL portable - initdb.exe"
    call :check_file "%PACKAGE_ROOT%db\bin\pg_isready.exe" "PostgreSQL portable - pg_isready.exe"
    call :check_file "%PACKAGE_ROOT%db\bin\createdb.exe" "PostgreSQL portable - createdb.exe"
    call :check_file "%PACKAGE_ROOT%db\bin\pg_dump.exe" "PostgreSQL portable - pg_dump.exe"
    call :check_file "%PACKAGE_ROOT%db\share\postgres.bki" "PostgreSQL portable - share\postgres.bki - required by initdb"
    call :check_file "%PACKAGE_ROOT%db\lib\dict_snowball.dll" "PostgreSQL portable - lib\dict_snowball.dll - required by initdb"
) else (
    echo [SKIP] PostgreSQL binaries - add manually before client handoff
)

echo.
if "!ERRORS!"=="0" (
    echo VALIDATION PASSED.
    if "%SKIP_POSTGRES%"=="1" (
        echo Remember to copy PostgreSQL db\bin\, db\share\, and db\lib\ before client delivery.
    ) else (
        echo Next: run 02_SETUP_FIRST_RUN.bat
    )
    endlocal
    if "%~1"=="" pause
    exit /b 0
)

echo VALIDATION FAILED - !ERRORS! issue(s) found.
endlocal
if "%~1"=="" pause
exit /b 1

:check_file
if exist "%~1" (
    echo [OK]   %~2
) else (
    echo [FAIL] %~2 - missing: %~1
    set /a ERRORS+=1
)
goto :eof
