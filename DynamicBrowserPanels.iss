; Dynamic Browser Panels - Simple Installer
; Inno Setup 6.0 or higher required

#define MyAppName "Dynamic Browser Panels"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Bil Hooper"
#define MyAppURL "https://github.com/bil-hooper/DynamicBrowserPanels"
#define MyAppExeName "DynamicBrowserPanels.exe"

[Setup]
AppId={{8F3A9C2D-5B1E-4D7C-9A3F-2E8B6C4D1A9F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=Installer
OutputBaseFilename=DynamicBrowserPanels-Setup
SetupIconFile=Rubik.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "bin\Release\net8.0-windows7.0\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Registry]
; .frm file association
Root: HKCR; Subkey: ".frm"; ValueData: "DynamicBrowserPanels.Layout"; Flags: uninsdeletevalue; ValueType: string; ValueName: ""
Root: HKCR; Subkey: "DynamicBrowserPanels.Layout"; ValueData: "Browser Layout File"; Flags: uninsdeletekey; ValueType: string; ValueName: ""
Root: HKCR; Subkey: "DynamicBrowserPanels.Layout\DefaultIcon"; ValueData: "{app}\{#MyAppExeName},0"; ValueType: string; ValueName: ""
Root: HKCR; Subkey: "DynamicBrowserPanels.Layout\shell\open\command"; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; ValueType: string; ValueName: ""

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent