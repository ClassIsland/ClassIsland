#define AppName "ClassIsland"
#ifndef AppVersion
#define AppVersion GetCmdParam("AppVersion", "2.0.0")
#endif
#define AppPublisher "ClassIsland"
#define AppURL "https://classisland.tech/"
#define AppExeName "ClassIsland.exe"

#ifndef SourceDir
#define SourceDir "D:/a/ClassIsland/ClassIsland/out_artifacts/out_appBase_windows_x64_full_folder"
#endif

[Files]
Source: "{#SourceDir}/*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

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
WizardStyle=modern dynamic windows11
#if Arch == "x64"
  ArchitecturesInstallIn64BitMode=x64
  ArchitecturesAllowed=x64
#elif Arch == "arm64"
  ArchitecturesInstallIn64BitMode=arm64
  ArchitecturesAllowed=arm64
#else
  ; x86 不需要特殊设置，默认即可
  ArchitecturesAllowed=x86 x64 arm64
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "languages\\ChineseSimplified.isl"
Name: "chinesetraditional"; MessagesFile: "languages\\ChineseTraditional.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
