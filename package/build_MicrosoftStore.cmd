@echo off

set app_version=1.0.0.0
set initial_directory=%cd%

REM create the output folder if it doesn't already exist.
mkdir Output > NUL 2>&1

REM Don't forget to update this when using a new WindowsAppSDK installed package.
SET PATH=%PATH%;"C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\"

echo.
echo ################################
echo Downloading latest DLSS dlls
echo ################################
echo.

dotnet run --project PrePackager\PrePackager.csproj || goto :error


echo.
echo ################################
echo Compiling app
echo ################################
echo.

dotnet publish "..\src\DLSS Swapper.csproj" ^
    -c Release_WindowsStore ^
    -r win10-x64 ^
    --self-contained false ^
    -p:Platform=x64 ^
    -p:PublishReadyToRun=true ^
    -p:WindowsAppSDKSelfContained=false ^
    -p:GenerateAppxPackageOnBuild=true ^
    -p:WindowsPackageType=MSIX ^
    -p:PublishTrimmed=false ^
    -p:AppxPackageDir=bin\publish\microsoft_store\ || goto :error  


echo.
echo ################################
echo Creating msixbundle
echo ################################
echo.

REM Build mapping file.
(
    echo [Files]
    echo "..\src\bin\publish\microsoft_store\DLSS Swapper_%app_version%_x64_Release_WindowsStore_Test\DLSS Swapper_%app_version%_x64_Release_WindowsStore.msix" "DLSS_Swapper_%app_version%_x64_Release_WindowsStore.msix"
) > ..\src\bin\publish\microsoft_store\mapping_file || goto :error

SET msix_bundle_file="..\src\bin\publish\microsoft_store\DLSS Swapper-%app_version%-WindowsStore.msixbundle"

REM Delete the bundle file if it already exists.
DEL %msix_bundle_file% > NUL 2>&1

REM Make the bundle file.
MakeAppx.exe bundle /o /f ..\src\bin\publish\microsoft_store\mapping_file /p %msix_bundle_file% || goto :error


echo.
echo ################################
echo Signing msixbundle
echo ################################
echo.

REM Sign the bundle file.
SignTool.exe sign /v /fd SHA256 /a %msix_bundle_file% || goto :error

REM Move the installer to the output folder.
move %msix_bundle_file% Output || goto :error


REM Everything is fine, go to the end of the file.
goto :EOF

REM If there was an error output this error message and navigate back to the initial directory 
:error
echo.
echo.
echo ERROR: Failed with error code %errorlevel%.
cd %initial_directory% > NUL 2>&1
exit /b %errorlevel%