@echo off

set app_version=1.1.3.1
set initial_directory=%cd%

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

dotnet publish "..\src\DLSS Swapper.csproj" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:Platform=x64 ^
    -p:PublishDir=bin\publish\unpackaged\ || goto :error

echo.
echo ################################
echo Building installer
echo ################################
echo.

:installer 
DEL NSIS\installer.exe > NUL 2>&1
DEL NSIS\FileList.nsh > NUL 2>&1

pwsh.exe .\NSIS\create_nsh_file_list.ps1 || goto :error

makensis.exe NSIS\Installer.nsi || goto :error
 
REM Move the installer to the output folder.
move NSIS\installer.exe "Output\DLSS.Swapper-%app_version%-installer.exe" || goto :error

REM Everything is fine, go to the end of the file.
goto :EOF

REM If there was an error output this error message and navigate back to the initial directory 
:error
echo.
echo.
echo ERROR: Failed with error code %errorlevel%.
cd %initial_directory% > NUL 2>&1
exit /b %errorlevel%