; Inno Setup script for Tarek Gaming Space.
; Build with build.ps1 (it publishes the app first), or: iscc installer\TarekGamingSpace.iss

#define AppName "Tarek Gaming Space"
#define AppExe "TarekGamingSpace.exe"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{6E2B7C4A-3F1D-4C1B-9B7E-2A5D8C4E1F70}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Tarek Gaming Space
DefaultDirName={autopf}\Tarek Gaming Space
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Per-user install: no admin prompt, works on locked-down laptops.
PrivilegesRequired=lowest
; Same mutex as the app, so setup asks to close it before updating or uninstalling.
AppMutex=TarekGamingSpace_SingleInstance
; Windows 7 SP1+ (the oldest Windows .NET Framework 4.5 and Inno Setup 6.4 run on). 32- and 64-bit both supported.
MinVersion=6.1sp1
OutputDir=Output
OutputBaseFilename=TarekGamingSpace-Setup-{#AppVersion}
SetupIconFile=..\src\TarekGamingSpace\Assets\app.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ShowLanguageDialog=auto

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ar"; MessagesFile: "compiler:Languages\Arabic.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\artifacts\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs; Excludes: "*.pdb"

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; The app writes its own "start with Windows" entry; remove it on uninstall.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "TarekGamingSpace"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
// .NET Framework 4.5 or later is built into Windows 8 and up; Windows 7 may lack it.
function IsDotNet45Installed(): Boolean;
var
  release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', release)
            and (release >= 378389);
end;

function InitializeSetup(): Boolean;
var
  code: Integer;
begin
  Result := True;
  if not IsDotNet45Installed() then
  begin
    if MsgBox('This app needs Microsoft .NET Framework 4.5 or later, which is missing on this computer.' + #13#10 +
              'Open the download page now? Install it, restart, then run this setup again.',
              mbConfirmation, MB_YESNO) = IDYES then
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet-framework/net48', '', '', SW_SHOWNORMAL, ewNoWait, code);
    Result := False;
  end;
end;
