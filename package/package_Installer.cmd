@echo off

call "%~dp0config.cmd"

echo.
echo ################################
echo Packaging installer
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