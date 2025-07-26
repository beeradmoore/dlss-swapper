@echo off

REM Update the static_manifest.json to the latest version.
REM We only do this in the build_all as to ensure both versions have THE EXACT same file.
call ..\update_manifest.cmd || goto :error

call build_Portable.cmd || goto :error
call package_Portable.cmd || goto :error
call build_Installer.cmd || goto :error
call package_Installer.cmd || goto :error

goto :EOF

:error
echo.
echo.
echo ERROR: Failed with error code %errorlevel%.
exit /b %errorlevel%