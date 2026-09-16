@echo off
REM Always On Top - 빌드 스크립트 (Windows)
REM .NET 8 SDK 가 필요합니다: https://dotnet.microsoft.com/download

setlocal
cd /d "%~dp0src\AlwaysOnTop"

echo [1/2] 복원 및 릴리스 빌드...
dotnet build -c Release
if errorlevel 1 goto :error

echo.
echo [2/2] 단일 실행 파일(self-contained) 생성...
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o "%~dp0dist"
if errorlevel 1 goto :error

echo.
echo 완료! 실행 파일: %~dp0dist\AlwaysOnTop.exe
goto :eof

:error
echo.
echo 빌드 실패. .NET 8 SDK 가 설치되어 있는지 확인하세요.
exit /b 1
