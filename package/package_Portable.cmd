@echo off

call "%~dp0config.cmd"

REM create the output folder if it doesn't already exist.
mkdir Output > NUL 2>&1

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