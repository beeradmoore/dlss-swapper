@echo off

REM Update this to your local path for cl.exe if needed.
set PATH=%PATH%;C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\14.38.33130\bin\Hostx64\x64\

cl.exe /O2 /Wall /FoRTSSHooksCompatibility.obj -nologo /c RTSSHooksCompatibility.c 
if %errorlevel% neq 0 exit /b %errorlevel%
dumpbin.exe /EXPORTS RTSSHooksCompatibility.obj
