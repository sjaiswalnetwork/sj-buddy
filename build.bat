@echo off
setlocal
cd /d "%~dp0"
REM SJ Buddy build script - needs nothing but Windows itself (uses the C# compiler that ships with .NET Framework 4.x).
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist dist mkdir dist
if not exist assets mkdir assets

if not exist assets\sjbuddy.ico (
  echo Generating icon...
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\make-icon.ps1" "%~dp0assets\sjbuddy.ico"
)

echo Compiling SJ Buddy...
"%CSC%" /nologo /target:winexe /optimize+ /codepage:65001 /platform:anycpu /out:"dist\SJ Buddy.exe" /win32icon:assets\sjbuddy.ico /win32manifest:src\app.manifest /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll src\*.cs
if errorlevel 1 (
  echo BUILD FAILED
  exit /b 1
)
echo Built "dist\SJ Buddy.exe"
endlocal
