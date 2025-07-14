@echo off

set app_version=1.2.0.3
set initial_directory=%cd%

set csproj_file=..\src\DLSS Swapper.csproj
set output_zip=Output\DLSS.Swapper-%app_version%-portable.zip

REM Delete bin and obj directory
rmdir /s /q ..\src\bin\
rmdir /s /q ..\src\obj\

REM create the output folder if it doesn't already exist.
mkdir Output > NUL 2>&1

echo.
echo ################################
echo Compiling app
echo ################################
echo.

dotnet publish "%csproj_file%" ^
	--runtime win-x64 ^
    --self-contained ^
    --configuration Release_Portable ^
    -p:PublishDir=bin\publish\portable\ || goto :error


echo.
echo ################################
echo Zipping app
echo ################################
echo.

pwsh.exe -ExecutionPolicy Bypass -Command Import-Module Microsoft.PowerShell.Archive; Compress-Archive -Force -Path "..\src\bin\publish\portable\*" -DestinationPath "%output_zip%" || goto :error

REM Everything is fine, go to the end of the file.
goto :end

REM If there was an error output this error message and navigate back to the initial directory 
:error
echo.
echo.
echo ERROR: Failed with error code %errorlevel%.
cd %initial_directory% > NUL 2>&1
exit /b %errorlevel%

:end
exit /b 0