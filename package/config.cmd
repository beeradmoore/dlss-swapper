@echo off

set app_version=1.2.1.1
set initial_directory=%cd%

set csproj_file=..\src\DLSS Swapper.csproj

set output_installer=Output\DLSS.Swapper-%app_version%-installer.exe
set output_zip=Output\DLSS.Swapper-%app_version%-portable.zip
