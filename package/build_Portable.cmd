@echo off

set app_version=1.0.0.0
set initial_directory=%cd%

REM create the output folder if it doesn't already exist.
mkdir Output > NUL 2>&1

echo.
echo ################################
echo Compiling app
echo ################################
echo.

dotnet publish "..\src\DLSS Swapper.csproj" ^
    -c Release ^
    -r win10-x64 ^
    --self-contained true ^
    -p:Platform=x64 ^
    -p:PublishReadyToRun=true ^
    -p:WindowsAppSDKSelfContained=true ^
    -p:WindowsPackageType=None ^
    -p:PublishTrimmed=false ^
    -p:DefineConstants="PORTABLE" ^
    -p:PublishDir=bin\publish\portable\ || goto :error

echo.
echo ################################
echo Zipping app
echo ################################
echo.

powershell Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process; Import-Module Microsoft.PowerShell.Archive; Compress-Archive -Force -Path "..\src\bin\publish\portable\*" -DestinationPath "Output\DLSS` Swapper-%app_version%-portable.zip" || goto :error

REM Everything is fine, go to the end of the file.
goto :EOF

REM If there was an error output this error message and navigate back to the initial directory 
:error
echo.
echo.
echo ERROR: Failed with error code %errorlevel%.
cd %initial_directory% > NUL 2>&1
exit /b %errorlevel%