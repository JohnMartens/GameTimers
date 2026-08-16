@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>&1
if errorlevel 1 (
  echo.
  echo .NET SDK is niet gevonden.
  echo Installeer de .NET 8 SDK en voer deze BAT daarna opnieuw uit.
  echo.
  pause
  exit /b 1
)
echo GameTimers wordt gebouwd...
dotnet publish GameTimers.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
if errorlevel 1 (
  echo.
  echo Er is een fout opgetreden tijdens het bouwen.
  pause
  exit /b 1
)
echo.
echo Klaar!
echo De EXE staat hier:
echo %~dp0publish\GameTimers.exe
echo.
