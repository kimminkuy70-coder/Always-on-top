@echo off
REM Always On Top - 개발용 실행 스크립트 (Windows, .NET 8 SDK 필요)
setlocal
cd /d "%~dp0src\AlwaysOnTop"
dotnet run -c Release
