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
call :check_file "%PACKAGE_ROOT%api\wwwroot\index.html" "Angular UI (wwwroot)"
call :check_file "%PACKAGE_ROOT%api\appsettings.json" "API appsettings"
call :check_file "%PACKAGE_ROOT%START.bat" "START.bat"
call :check_file "%PACKAGE_ROOT%STOP.bat" "STOP.bat"
call :check_file "%PACKAGE_ROOT%SETUP_FIRST_RUN.bat" "SETUP_FIRST_RUN.bat"
call :check_file "%PACKAGE_ROOT%REGISTER_THIS_PC.bat" "REGISTER_THIS_PC.bat"
call :check_file "%PACKAGE_ROOT%DAILY_BACKUP.bat" "DAILY_BACKUP.bat"
call :check_file "%PACKAGE_ROOT%CLIENT_SETUP_AND_START.txt" "Client setup guide"
call :check_file "%PACKAGE_ROOT%tools\ABR.Secrets.exe" "Secrets tool"

if "%SKIP_POSTGRES%"=="0" (
    call :check_file "%PACKAGE_ROOT%db\bin\pg_ctl.exe" "PostgreSQL portable (pg_ctl.exe)"
    call :check_file "%PACKAGE_ROOT%db\bin\initdb.exe" "PostgreSQL portable (initdb.exe)"
    call :check_file "%PACKAGE_ROOT%db\bin\pg_isready.exe" "PostgreSQL portable (pg_isready.exe)"
    call :check_file "%PACKAGE_ROOT%db\bin\createdb.exe" "PostgreSQL portable (createdb.exe)"
    call :check_file "%PACKAGE_ROOT%db\bin\pg_dump.exe" "PostgreSQL portable (pg_dump.exe)"
) else (
    echo [SKIP] PostgreSQL binaries - add manually before client handoff
)

echo.
if "!ERRORS!"=="0" (
    echo VALIDATION PASSED.
    if "%SKIP_POSTGRES%"=="1" (
        echo Remember to copy PostgreSQL 15 binaries to db\bin\ before client delivery.
    )
    endlocal
    exit /b 0
)

echo VALIDATION FAILED - !ERRORS! issue(s) found.
endlocal
exit /b 1

:check_file
if exist "%~1" (
    echo [OK]   %~2
) else (
    echo [FAIL] %~2 - missing: %~1
    set /a ERRORS+=1
)
goto :eof
