@echo off

call build_Portable.cmd net8 || goto :error
call build_Portable.cmd || goto :error
call build_Installer.cmd net8 || goto :error
call build_Installer.cmd || goto :error

goto :EOF

:error
echo.
echo.
echo ERROR: Failed with error code %errorlevel%.
exit /b %errorlevel%