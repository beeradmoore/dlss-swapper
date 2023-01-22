# Package instructions
This document assumes you have a correct development environment setup. Each package step may require additional tools. 

Final output for all 3 build scripts is `Output/`.

## build_Portable.cmd
Builds a portable zipped app. This requires [powershell](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows) installed to complete the actual zip step.

## build_Unpackaged.cmd
Builds an installable app. This requires [Nullsoft Scriptable Install System](https://nsis.sourceforge.io/Main_Page) installed. The script that is run to build the installer is in `NSIS\Installer.nsi`.

## build_MicrosoftStore.cmd
Builds a msixbundle used to distribute to the Microsoft Store. 

Building with this will first run PrePackager project which downloads the latest DLSS dlls to be bundled into the msixbundle.

Both packaging (`MakeAppx.exe`) and signing (`SignTool.exe`) the msixbundle require these to be in the path. Because of this the top of the script runs
```
SET PATH=%PATH%;"C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\"
```
If you are not using Windows SDK 10.0.22621.0 or have it installed on another drive this will need to be updated.

Signing will likely fail due to `PackageCertificateThumbprint` in `DLSS Swapper.csproj` not mapping a temp certificate you made. So complete this you may have to make changes to `DLSS Swapper.csproj`, `app.manifest`, and `Package.StoreAssociation.xml` to reflect your local dev certificate and publisher ID.