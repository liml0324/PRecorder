; ============================================================
; PRecorder Inno Setup Script
; Compile with: ISCC.exe Packaging\setup.iss
; Requires Inno Setup 6+: https://jrsoftware.org/isinfo.php
; ============================================================

#define AppName       "PRecorder"
#define AppVersion    "1.0.0.0"
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
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch PRecorder"; Flags: nowait postinstall skipifsilent
