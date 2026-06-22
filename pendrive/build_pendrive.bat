@echo off
setlocal EnableExtensions
cd /d "%~dp0.."

echo === ABR Pendrive Build ===

set ROOT=%~dp0
set OUT=%ROOT%package

if exist "%OUT%" rmdir /s /q "%OUT%"
mkdir "%OUT%\api"
mkdir "%OUT%\frontend"
mkdir "%OUT%\db"
mkdir "%OUT%\config"
mkdir "%OUT%\logs"
mkdir "%OUT%\exports"
mkdir "%OUT%\backup"

echo [1/4] Building Angular production bundle...
pushd frontend
call npm run build -- --configuration production
if errorlevel 1 exit /b 1
xcopy /E /I /Y dist\abr-frontend\browser\* "%OUT%\frontend\"
popd

echo [2/4] Publishing ASP.NET Core API (win-x64 self-contained)...
dotnet publish backend\ABR.Api\ABR.Api.csproj -c Release -r win-x64 --self-contained true -o "%OUT%\api"
if errorlevel 1 exit /b 1

echo [3/4] Copying launcher scripts and config...
copy /Y pendrive\START.bat "%OUT%\"
copy /Y pendrive\STOP.bat "%OUT%\"
copy /Y pendrive\SETUP_FIRST_RUN.bat "%OUT%\"
copy /Y pendrive\DAILY_BACKUP.bat "%OUT%\"

if exist backend\ABR.Api\appsettings.Pendrive.json (
  copy /Y backend\ABR.Api\appsettings.Pendrive.json "%OUT%\config\appsettings.json"
) else (
  echo {^"ConnectionStrings^":{^"DefaultConnection^":^"Host=127.0.0.1;Port=5433;Database=abr_db;Username=postgres;Password=postgres^"},^"Security^":{^"EnforceDeviceLock^":true,^"LicenseFilePath^":^"config/device.lic^"}} > "%OUT%\config\appsettings.json"
)

echo [4/4] Done. Package at: %OUT%
echo Place portable PostgreSQL 15 binaries under %OUT%\db\
echo Run SETUP_FIRST_RUN.bat on first use.
endlocal
