@echo off

set app_version=1.1.4.0
set initial_directory=%cd%

set build_mode=net9
set csproj_file=..\src\DLSS Swapper.csproj
set output_zip=Output\DLSS.Swapper-%app_version%-portable.zip
REM Check if argument is net8, if it is anything else fail
if "%1"=="" (
    REM Do nothing, continue with net9    
) else (
    if "%1"=="net8" (
        set build_mode=net8
        set csproj_file=..\src\DLSS Swapper.net8.csproj
        set output_zip=Output\DLSS.Swapper-%app_version%-portable-net8.zip
    ) else (
        echo Other argument given, only expecting "net8"
        goto :EOF
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
    -p:DefineConstants="PORTABLE" ^
    -p:PublishDir=bin\publish\portable\ || goto :error


echo.
echo ################################
echo Zipping app
echo ################################
echo.

pwsh.exe -ExecutionPolicy Bypass -Command Import-Module Microsoft.PowerShell.Archive; Compress-Archive -Force -Path "..\src\bin\publish\portable\*" -DestinationPath "%output_zip%" || goto :error

REM Everything is fine, go to the end of the file.
goto :EOF

REM If there was an error output this error message and navigate back to the initial directory 
:error
echo.
echo.
echo ERROR: Failed with error code %errorlevel%.
cd %initial_directory% > NUL 2>&1
exit /b %errorlevel%