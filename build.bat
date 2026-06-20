@echo off
echo.
echo  SAI Guard - Build
echo  ==================
echo.

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo ERROR: csc.exe not found. This should not happen on Windows 10+.
    pause
    exit /b 1
)

echo Compiler: %CSC%
echo.

"%CSC%" ^
    /out:SAIGuard.exe ^
    /target:winexe ^
    /optimize+ ^
    /nologo ^
    /r:System.Windows.Forms.dll ^
    /r:System.Drawing.dll ^
    SAIGuard.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo  ====================================
    echo   BUILD OK - SAIGuard.exe is ready
    echo  ====================================
    echo.
    for %%F in (SAIGuard.exe) do echo  Size: %%~zF bytes
    echo.
    echo  Just double-click SAIGuard.exe to run.
    echo  It will appear in your system tray.
    echo.
) else (
    echo.
    echo  BUILD FAILED - check errors above.
)
pause
