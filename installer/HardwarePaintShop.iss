#define MyAppName "Hardware Paint Shop System"
#define MyAppArabicName "نظام إدارة محل الحدايد والبوهيات"
#define MyAppVersion "1.1.0-rc1"
#define MyAppPublisher "Hardware Paint Shop"
#define MyAppExeName "HardwarePaintShop.Desktop.exe"

[Setup]
AppId={{D2D1A067-6CE8-4BDD-A8A9-6A4DF8058301}
AppName={#MyAppArabicName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\HardwarePaintShop
DefaultGroupName={#MyAppArabicName}
OutputDir=..\artifacts\installer
OutputBaseFilename=HardwarePaintShopSystem_Setup_{#MyAppVersion}_win-x64
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب"; GroupDescription: "اختصارات إضافية:"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{commonappdata}\HardwarePaintShop\Backups"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppArabicName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppArabicName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=&quot;Hardware Paint Shop Mobile API&quot; dir=in action=allow protocol=TCP localport=5000 profile=private"; Flags: runhidden; StatusMsg: "إعداد اتصال تطبيق الموبايل..."
Filename: "{app}\{#MyAppExeName}"; Description: "تشغيل البرنامج"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=&quot;Hardware Paint Shop Mobile API&quot;"; Flags: runhidden

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  if not RegKeyExists(HKLM64, 'SOFTWARE\PostgreSQL\Installations') then
    MsgBox('تنبيه: يحتاج البرنامج PostgreSQL 14 أو أحدث. يمكنك إكمال التثبيت، لكن يجب تثبيت PostgreSQL قبل أول تشغيل.', mbInformation, MB_OK);
end;
