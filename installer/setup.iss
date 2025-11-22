#define AppName "ClassIsland"
#ifndef AppVersion
#define AppVersion GetCmdParam("AppVersion", "2.0.0")
#endif
#define AppPublisher "ClassIsland"
#define AppURL "https://classisland.tech/"
#define AppExeName "ClassIsland.exe"
#define SourceDir GetEnv("GITHUB_WORKSPACE") + "\\out_pack\\pack"

[Setup]
AppId={{09B0F8C9-4C5C-4762-9288-7D5C3F72B09D}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
DisableProgramGroupPage=yes
LicenseFile={#GetEnv("GITHUB_WORKSPACE") + "\\LICENSE.txt"}
SetupIconFile={#GetEnv("GITHUB_WORKSPACE") + "\\ClassIsland\\Assets\\AppLogo.ico"}
SolidCompression=yes
; WizardStyle=modern dynamic windows11

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "languages\\ChineseSimplified.isl"
Name: "chinesetraditional"; MessagesFile: "languages\\ChineseTraditional.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
