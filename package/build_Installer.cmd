@echo off

set app_version=1.1.6.0
set initial_directory=%cd%

set build_mode=net9
set csproj_file=..\src\DLSS Swapper.csproj
set output_installer=Output\DLSS.Swapper-%app_version%-installer.exe
REM Check if argument is net8, if it is anything else fail
if "%1"=="" (
    REM Do nothing, continue with net9    
) else (
    if "%1"=="net8" (
        set build_mode=net8
        set csproj_file=..\src\DLSS Swapper.net8.csproj
        set output_installer=Output\DLSS.Swapper-%app_version%-installer-net8.exe
    ) else (
        echo Other argument given, only expecting "net8"
        goto :error
    )
)

REM Delete bin and obj directory
rmdir /s /q ..\src\bin\
rmdir /s /q ..\src\obj\

REM create the output folder if it doesn't already exist.
mkdir Output > NUL 2>&1

echo.
echo ################################
echo Compiling app - %build_mode%
echo ################################
echo.

REM If building for net8 then create new csproj
if "%1"=="net8" (

pwsh -Command ^
"^
$oldString = 'net9.0-windows'; ^
$newString = 'net8.0-windows'; ^
$content = Get-Content -Path '..\src\DLSS Swapper.csproj'; ^
$content = $content -replace $oldString, $newString; ^
Set-Content -Path '%csproj_file%' -Value $content; ^
"
)

dotnet publish "%csproj_file%" ^
	--runtime win-x64 ^
    --self-contained ^
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
move NSIS\installer.exe "%output_installer%" || goto :error

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