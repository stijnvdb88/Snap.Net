#define MyAppName "Snap.Net"
#define MyAppVersion GetEnv("SNAP_VERSION")
#define MyAppURL "https://github.com/stijnvdb88/Snap.Net"

; https://github.com/domgho/innodependencyinstaller
#include "scripts\products.iss"
#include "scripts\products\stringversion.iss"
#include "scripts\products\winversion.iss"
#include "scripts\products\fileversion.iss"
#include "scripts\products\msiproduct.iss"
#include "scripts\products\dotnetfxversion.iss"
#include "scripts\products\dotnetfx47.iss"
#include "scripts\products\vcredist2017.iss"


[Files]
Source: ..\bin\Release\Snap.Net.exe; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\bin\Release\Snap.Net.exe.config; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\bin\Release\SnapClient\*; DestDir: "{app}/SnapClient"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: ..\bin\Release\CliWrap.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\bin\Release\ControlzEx.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\bin\Release\Gma.System.MouseKeyHook.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\bin\Release\Hardcodet.Wpf.TaskbarNotification.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\bin\Release\MahApps.*.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\bin\Release\MessagePack.Annotations.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\bin\Release\MessagePack.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\bin\Release\Microsoft.*.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\bin\Release\Nerdbank.Streams.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\bin\Release\Newtonsoft.Json.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\bin\Release\NLog.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\bin\Release\StreamJsonRpc.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion
Source: ..\bin\Release\System.*.dll; DestDir: {app}; Flags: overwritereadonly ignoreversion replacesameversion

[Setup]
AppId={#MyAppName}
AppName={#MyAppName}
AppMutex=978C614F-708E-4E1A-B201-565925725DBA
AppPublisher={#MyAppName}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppVerName={#MyAppName} {#MyAppVersion}
AppVersion={#MyAppVersion}
ArchitecturesInstallIn64BitMode=x64
Compression=lzma
SolidCompression=yes
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableReadyPage=no
DisableReadyMemo=no
LicenseFile=..\LICENSE
LanguageDetectionMethod=uilanguage
MinVersion=0,6.1.7600
OutputBaseFilename={#MyAppName}-{#MyAppVersion}-Setup
OutputDir=..\bin
SetupIconFile=..\Assets\snapcast.ico
UninstallDisplayIcon={app}\{#MyAppName}.exe
Uninstallable=true
VersionInfoCompany={#MyAppName}
VersionInfoProductName={#MyAppName}
VersionInfoTextVersion={#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
WizardImageFile=assets/large.bmp
WizardSmallImageFile=assets/icon.bmp

#include "scripts\lang\english.iss"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppName}.exe"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppName}.exe"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppName}.exe"; Tasks: quicklaunchicon


[Run]
Filename: "{app}\{#MyAppName}.exe"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[CustomMessages]
DependenciesDir=Dependencies

[Code]
function InitializeSetup(): boolean;
begin
	// initialize windows version
	initwinversion();
  	Result := true;

  vcredist2017('14.1');
  dotnetfx47(70);

end;
