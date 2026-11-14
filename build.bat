@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
set PC=%WINDIR%\Microsoft.NET\assembly\GAC_64\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll
if not exist "%PC%" set PC=%WINDIR%\Microsoft.NET\assembly\GAC_32\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll
set WB=%WINDIR%\Microsoft.NET\assembly\GAC_MSIL\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll
"%CSC%" -nologo -target:winexe -optimize+ -codepage:65001 -win32manifest:app.manifest -out:MuPlayer.exe -r:System.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:"%PC%" -r:"%WB%" Program.cs
if errorlevel 1 (pause) else (echo OK: MuPlayer.exe)