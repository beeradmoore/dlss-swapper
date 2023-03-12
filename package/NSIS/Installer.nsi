!include "MUI2.nsh"
!include LogicLib.nsh



 # define name of installer
OutFile "installer.exe"

!define UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\DLSS Swapper"


Function .onInit
  StrCpy $INSTDIR  "$PROGRAMFILES64\DLSS Swapper\"
  ClearErrors
  ReadRegStr $0 SHCTX "${UNINST_KEY}" "InstallLocation"
  ${If} ${Errors}

  ${Else}
    StrCpy $INSTDIR "$0\"
  ${EndIf}


FunctionEnd

# For removing Start Menu shortcut in Windows 7
#RequestExecutionLevel user
RequestExecutionLevel highest

; App version information
Name "DLSS Swapper"
!define MUI_ICON "..\..\src\Assets\icon.ico"
!define MUI_VERSION "1.0.1.0"
!define MUI_PRODUCT "DLSS Swapper"
VIProductVersion "1.0.1.0"
VIAddVersionKey "ProductName" "DLSS Swapper"
VIAddVersionKey "ProductVersion" "1.0.1.0"
VIAddVersionKey "FileDescription" "DLSS Swapper installer"
VIAddVersionKey "FileVersion" "1.0.1.0"

; Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
 
; These indented statements modify settings for MUI_PAGE_FINISH
!define MUI_FINISHPAGE_NOAUTOCLOSE
!define MUI_FINISHPAGE_RUN
!define MUI_FINISHPAGE_RUN_CHECKED
!define MUI_FINISHPAGE_RUN_TEXT "Launch now"
!define MUI_FINISHPAGE_RUN_FUNCTION "LaunchLink"
!insertmacro MUI_PAGE_FINISH

; Uninstaller pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES


; Languages
!insertmacro MUI_LANGUAGE "English"



# start default section
Section
  # set the installation directory as the destination for the following actions
  SetOutPath $INSTDIR

  File /r "..\..\src\bin\publish\unpackaged\*"
  
  # create the uninstaller
  WriteUninstaller "$INSTDIR\uninstall.exe"
 
  # create a shortcut named "new shortcut" in the start menu programs directory
  # point the new shortcut at the program uninstaller
  CreateShortcut "$SMPROGRAMS\DLSS Swapper.lnk" "$INSTDIR\DLSS Swapper.exe"

  WriteRegStr SHCTX "${UNINST_KEY}" "DisplayName" "DLSS Swapper"
  WriteRegStr SHCTX "${UNINST_KEY}" "UninstallString" "$\"$INSTDIR\uninstall.exe$\""
  WriteRegStr SHCTX "${UNINST_KEY}" "InstallLocation" $INSTDIR


SectionEnd



# uninstaller section start
Section "Uninstall"
  #$LOCALAPPDATA
  #$APPDATAS

  # This is a little dumb, but I'd rather be safe than sorry. 
  # We check both the reg file for install directory and the
  # current path the uninstall is executing from. If they don't
  # match then we don't uninstall. 
  #
  # I nuked all the loose files on my C drive by not checking 
  # if RegInstallDir is empty or not.
  ClearErrors
  ReadRegStr $0 SHCTX "${UNINST_KEY}" "InstallLocation"
  ${If} ${Errors}
    MessageBox MB_OK "Could not determine install location."
    Abort
  ${EndIf}

  # Copy the value to a varaible.
  Var /GLOBAL RegInstallDir
  StrCpy $RegInstallDir $0
  
  # If the variable is an empty string we abort.
  ${If} $RegInstallDir == ""
    MessageBox MB_OK "Could not determine install location."
    Abort
  ${EndIf}

  # If the reg install dir does not match where we are executing
  # from then we abort.
  ${If} $RegInstallDir != $INSTDIR
    MessageBox MB_OK "Invalid install dir"
    Abort
  ${EndIf}

  Delete "$RegInstallDir\uninstall.exe"
  Delete "$RegInstallDir\*.*"
  DeleteRegKey SHCTX "${UNINST_KEY}"

  # This is not recommended but I believe at this point we have verified
  # that this path is what we expect it to be and not the root of a drive.
  RMDir /r "$RegInstallDir"

  # Remove start menu shortcut.
  Delete "$SMPROGRAMS\DLSS Swapper.lnk"
SectionEnd


Function LaunchLink
  ExecShell "" "$SMPROGRAMS\DLSS Swapper.lnk"
FunctionEnd

