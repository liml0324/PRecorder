; ============================================================
; PRecorder Inno Setup Script -- PORTABLE (framework-dependent)
; Requires .NET 10 runtime installed separately.
; ============================================================

#define AppName       "PRecorder"
#define AppVersion    "1.0.0"
#define AppPublisher  "PRecorder"
#define OutputDir     "..\bin"
#define SourceDir     "..\bin\publish_fd"

[Setup]
AppId={{B5F1A2C3-D4E5-4F6A-7890-ABCDEF123456}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppVerName={#AppName} {#AppVersion} (Portable)
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir={#OutputDir}
OutputBaseFilename=PRecorder_Setup_{#AppVersion}_x64_fd
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
Source: "..\icon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "wscript.exe"; Parameters: """{app}\PRecorder.vbs"""; IconFilename: "{app}\icon.ico"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "wscript.exe"; Parameters: """{app}\PRecorder.vbs"""; IconFilename: "{app}\icon.ico"; Tasks: desktopicon

[Run]
Filename: "wscript.exe"; Parameters: """{app}\PRecorder.vbs"""; Description: "Launch PRecorder (.NET 10 required)"; Flags: nowait postinstall skipifsilent
