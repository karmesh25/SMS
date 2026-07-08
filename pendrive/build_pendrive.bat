@echo off
setlocal EnableExtensions
cd /d "%~dp0.."

echo === ABR Pendrive Build ===

set ROOT=%~dp0
set OUT=%ROOT%package

if exist "%OUT%" rmdir /s /q "%OUT%"
mkdir "%OUT%\api"
mkdir "%OUT%\api\wwwroot"
mkdir "%OUT%\db\bin"
mkdir "%OUT%\config"
mkdir "%OUT%\logs"
mkdir "%OUT%\exports"
mkdir "%OUT%\backup"
mkdir "%OUT%\tools"

echo [1/6] Building Angular production bundle...
pushd frontend
call npm run build -- --configuration production
if errorlevel 1 exit /b 1
popd

echo [2/6] Publishing ASP.NET Core API (win-x64 self-contained)...
dotnet publish backend\ABR.Api\ABR.Api.csproj -c Release -r win-x64 --self-contained true -o "%OUT%\api"
if errorlevel 1 exit /b 1

echo [3/6] Publishing secrets tool (win-x64 self-contained)...
dotnet publish backend\ABR.Tools.Secrets\ABR.Tools.Secrets.csproj -c Release -r win-x64 --self-contained true -o "%OUT%\tools"
if errorlevel 1 exit /b 1

echo [4/6] Copying frontend to api\wwwroot...
xcopy /E /I /Y frontend\dist\abr-frontend\browser\* "%OUT%\api\wwwroot\"
if errorlevel 1 exit /b 1

echo [5/6] Copying launcher scripts, config, and docs...
copy /Y pendrive\START.bat "%OUT%\"
copy /Y pendrive\STOP.bat "%OUT%\"
copy /Y pendrive\SETUP_FIRST_RUN.bat "%OUT%\"
copy /Y pendrive\REGISTER_THIS_PC.bat "%OUT%\"
copy /Y pendrive\UPGRADE_SECURITY.bat "%OUT%\"
copy /Y pendrive\DAILY_BACKUP.bat "%OUT%\"
copy /Y pendrive\load_master_password.bat "%OUT%\"
copy /Y pendrive\prompt_master_password.ps1 "%OUT%\"
copy /Y pendrive\prompt_setup_master_password.ps1 "%OUT%\"
copy /Y pendrive\CLIENT_SETUP_AND_START.txt "%OUT%\"
copy /Y pendrive\validate_package.bat "%OUT%\"

if exist backend\ABR.Api\appsettings.Pendrive.json (
  copy /Y backend\ABR.Api\appsettings.Pendrive.json "%OUT%\api\appsettings.json"
) else (
  echo ERROR: appsettings.Pendrive.json not found.
  exit /b 1
)

if exist pendrive\db\README_POSTGRES.txt (
  copy /Y pendrive\db\README_POSTGRES.txt "%OUT%\db\README_POSTGRES.txt"
)

echo [6/6] Validating package structure...
call pendrive\validate_package.bat skip-postgres "%OUT%"
if errorlevel 1 exit /b 1

echo.
echo === Build complete ===
echo Package folder: %OUT%
echo.
echo NEXT STEPS (before giving USB to client):
echo   1. Copy PostgreSQL 15 portable binaries to %OUT%\db\bin\
echo      See %OUT%\db\README_POSTGRES.txt
echo   2. Copy everything inside package\ to the USB drive root
echo   3. On client PC run SETUP_FIRST_RUN.bat (choose master password)
echo   4. Run REGISTER_THIS_PC.bat, then STOP.bat, then START.bat
echo   5. Run validate_package.bat on the USB to confirm readiness
echo.
endlocal
