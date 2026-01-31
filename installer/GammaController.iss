; Inno Setup Script for Gamma Controller
; Download Inno Setup from: https://jrsoftware.org/isdl.php

#define MyAppName "Gamma Controller"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Omar Moustafa"
#define MyAppURL "https://github.com/OmarMoust/gamma-controller"
#define MyAppExeName "GammaController.exe"

[Setup]
; App identity
AppId={{8F4E9B2A-3C5D-4E6F-8A9B-1C2D3E4F5A6B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Installation settings
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Output settings
OutputDir=..\dist
OutputBaseFilename=GammaController-Setup-{#MyAppVersion}
SetupIconFile=GammaController.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; Compression
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

; Visual
WizardStyle=modern
WizardSizePercent=100

; Misc
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start with Windows"; GroupDescription: "Startup:"; Flags: checkedonce

[Files]
Source: "..\publish\GammaController.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "GammaController.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Start Menu shortcut
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\GammaController.ico"
; Desktop shortcut (optional)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\GammaController.ico"; Tasks: desktopicon
; Startup shortcut (runs minimized)
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--minimized"; IconFilename: "{app}\GammaController.ico"; Tasks: startupicon

[Registry]
; Register as a proper Windows app for search
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\GammaController.exe"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\GammaController.exe"; ValueType: string; ValueName: "Path"; ValueData: "{app}"; Flags: uninsdeletekey

[Run]
; Option to run after install
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Clean up app data on uninstall (optional - commented out to preserve settings)
; Type: filesandordirs; Name: "{localappdata}\GammaController"

[Code]
// Close the app before uninstalling
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  // Try to close the app gracefully
  Exec('taskkill', '/f /im GammaController.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

