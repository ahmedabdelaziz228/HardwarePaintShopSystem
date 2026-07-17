# تشغيل نسخة 1.1 Mobile RC1

## 1) بناء وتشغيل برنامج الكمبيوتر

افتح PowerShell داخل مجلد المشروع ونفّذ:

```powershell
dotnet restore .\HardwarePaintShopSystem.sln
dotnet build .\HardwarePaintShopSystem.sln -c Debug --no-restore
dotnet test .\HardwarePaintShopSystem.sln -c Debug --no-build
dotnet run --project .\src\HardwarePaintShop.Desktop\HardwarePaintShop.Desktop.csproj
```

غيّر كلمة مرور `admin` عند أول دخول. قاعدة البيانات الجديدة تعطي دور Owner صلاحيات الموبايل تلقائيًا.

## 2) تشغيل API أثناء التطوير

اترك برنامج الكمبيوتر مفتوحًا، وافتح PowerShell آخر:

```powershell
dotnet run --project .\src\HardwarePaintShop.Api\HardwarePaintShop.Api.csproj
```

اختبره:

```powershell
Invoke-RestMethod http://localhost:5000/api/health
```

اعرف عنوان الكمبيوتر داخل الشبكة باستخدام `ipconfig`. استخدم IPv4 الخاص بك داخل تطبيق الموبايل، مثل:

```text
http://192.168.1.20:5000
```

لا تستخدم `localhost` على هاتف حقيقي. محاكي Android يستخدم `http://10.0.2.2:5000`.

## 3) تجهيز وتشغيل تطبيق Android

ثبّت Flutter stable وAndroid Studio، ثم:

```powershell
cd .\mobile\HardwarePaintShopMobile
flutter create --platforms=android --org com.hardwarepaintshop .
flutter pub get
flutter analyze
flutter test
flutter run
```

لإخراج APK:

```powershell
flutter build apk --release
```

المسار المتوقع:

```text
mobile\HardwarePaintShopMobile\build\app\outputs\flutter-apk\app-release.apk
```

## 4) إخراج EXE وInstaller

النشر يشمل برنامج الكمبيوتر وAPI الذي يبدأ تلقائيًا مع البرنامج المنشور:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1
```

ولإنشاء Installer بعد تثبيت Inno Setup 6:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1 -BuildInstaller
```

الـInstaller يضيف قاعدة Windows Firewall للمنفذ 5000 على الشبكات الخاصة فقط. لا تفتح المنفذ مباشرة على الإنترنت؛ استخدم VPN موثوقًا لو احتجت دخولًا من خارج المحل.

## لو ظهر خطأ

أرسل أول خطأ كامل من أمر `dotnet build` أو `flutter analyze` بدون اختصار. لا تشغّل `dotnet test --no-build` قبل نجاح البناء، لأن ملفات الاختبارات لن تكون موجودة لو فشل البناء.
