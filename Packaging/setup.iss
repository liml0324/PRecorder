; ============================================================
; PRecorder Inno Setup Script
; Compile with: ISCC.exe Packaging\setup.iss
; Requires Inno Setup 6+: https://jrsoftware.org/isinfo.php
; ============================================================

#define AppName       "PRecorder"
#define AppVersion    "1.0.1"
#define AppPublisher  "PRecorder"
#define AppURL        "https://github.com"
#define AppExeName    "PRecorder.exe"
#define OutputDir     "..\bin"
#define SourceDir     "..\bin\publish_full"

[Setup]
AppId={{B5F1A2C3-D4E5-4F6A-7890-ABCDEF123456}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir={#OutputDir}
OutputBaseFilename=PRecorder_Setup_{#AppVersion}_x64
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "zh"; MessagesFile: "ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\icon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\icon.ico"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\icon.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch PRecorder"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if ActiveLanguage = 'zh' then
      SaveStringToFile(ExpandConstant('{app}\language.txt'), 'zh-CN', False)
    else
      SaveStringToFile(ExpandConstant('{app}\language.txt'), 'en-US', False);
  end;
end;
