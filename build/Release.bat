@echo off

set msbuild="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

set VERSION=1.1.0

REM --------------------------------
REM Build solution
REM --------------------------------

%msbuild% Target-Build.msbuild
set BUILD_STATUS=%ERRORLEVEL% 

if %BUILD_STATUS%==0 goto continuebuild
if not %BUILD_STATUS%==0 goto failbuild
 
:failbuild
pause
exit /b 1 

:continuebuild

REM --------------------------------
REM Package NuGet 
REM --------------------------------

%msbuild% Target-NuGet.msbuild
BUILD_STATUS=%ERRORLEVEL% 

if %BUILD_STATUS%==0 goto continue 
if not %BUILD_STATUS%==0 goto failpackage 
 
:failpackage
pause
exit /b 1 

:continue

pause

exit /b 0 
