#define MyAppName "مدير كلمات المرور"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "babfialbi91-design"
#define MyAppExeName "PasswordManager.exe"
#define MyAppId "E22B31B4-D2F9-48DD-A331-1CD94E98FA71"

[Setup]
AppId={{E22B31B4-D2F9-48DD-A331-1CD94E98FA71}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/babfialbi91-design/PAS-MAN-RELEASES
AppSupportURL=https://github.com/babfialbi91-design/PAS-MAN-RELEASES
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
SetupIconFile=..\src\PasswordManager.App\assets\icon.ico
WizardStyle=modern
WizardSizePercent=115
OutputDir=..\dist\installer
OutputBaseFilename=PasswordManagerSetup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
ShowLanguageDialog=no
VersionInfoVersion=1.0.0
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion=1.0.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=مدير كلمات المرور - Password Manager
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[Tasks]
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب"; GroupDescription: "اختصارات إضافية:"; Flags: unchecked
Name: "autostart"; Description: "تشغيل التطبيق عند بدء التشغيل"; GroupDescription: "خيارات التشغيل:"; Flags: unchecked

[Files]
Source: "..\dist\publish\PasswordManager.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "PasswordManager"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "تشغيل {#MyAppName} الآن"; Flags: nowait postinstall skipifsilent
