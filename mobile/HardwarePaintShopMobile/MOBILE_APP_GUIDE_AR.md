# دليل تطبيق الموبايل — Hardware Paint Shop Mobile

## 1. فكرة التطبيق

تطبيق Android مرافق لبرنامج إدارة المحل على الكمبيوتر. يتصل بـAPI محلي يعمل على اللاب أو الكمبيوتر الرئيسي داخل نفس شبكة Wi-Fi. التطبيق مخصص للوصول السريع إلى بيانات المحل وتنفيذ عمليات ميدانية بدون الحاجة للجلوس أمام الكمبيوتر.

برنامج الكمبيوتر وقاعدة PostgreSQL يظلان المصدر الأساسي للحقيقة، بينما الموبايل يعمل كواجهة آمنة محدودة بالصلاحيات.

## 2. الوظائف الحالية

- تسجيل الدخول بحساب من برنامج المحل.
- إدخال وحفظ عنوان API الخاص بالكمبيوتر.
- لوحة معلومات مختصرة.
- البحث عن المنتجات بالاسم أو الكود أو الباركود أو السيريال.
- مسح الباركود باستخدام الكاميرا.
- عرض تفاصيل المنتج والأسعار والرصيد والصورة.
- إضافة منتج وصورة وباركود وسعر مبدئي.
- إجراء تسوية مخزون عند امتلاك الصلاحية.
- البحث عن العملاء وعرض كشف الحساب وآخر الفواتير.
- تسجيل تحصيل من العميل عند امتلاك الصلاحية.
- عرض التنبيهات وتعليمها كمقروءة.
- تخزين محلي للمنتجات والعملاء والتنبيهات والباركود.
- Queue للعمليات عند انقطاع الاتصال ثم رفعها بالمزامنة.
- إظهار حالة الاتصال وآخر مزامنة والعمليات المعلقة.

## 3. التقنيات المستخدمة

| الجزء | التقنية |
| --- | --- |
| الواجهة | Flutter Material 3 / RTL |
| إدارة الحالة | Provider + ChangeNotifier |
| الاتصال | package:http |
| التخزين المحلي | SQLite عبر sqflite |
| Token | flutter_secure_storage |
| الإعدادات المحلية | shared_preferences |
| الباركود | mobile_scanner |
| الكاميرا | image_picker |
| التنسيق المالي | intl |

لم تتم إضافة Bloc أو Riverpod أو مكتبة Routing خارجية؛ المشروع مستمر على الأدوات الموجودة بالفعل.

## 4. المعمارية الحالية

مرحلة Auth وCore انتقلت إلى تنظيم Feature-first، وباقي الشاشات يتم نقلها تدريجيًا بدون كسر التطبيق:

```text
lib/
├── main.dart
├── app/
│   ├── app.dart
│   └── app_bootstrap.dart
├── core/
│   ├── config/
│   ├── errors/
│   ├── network/
│   ├── theme/
│   └── utils/
├── data/
│   └── local/app_database.dart
├── features/
│   ├── auth/
│   │   ├── data/
│   │   ├── logic/
│   │   └── presentation/
│   └── shell/presentation/home_shell.dart
├── routes/
│   └── auth_gate.dart
├── app_state.dart
└── models.dart
```

الحالة الانتقالية مقصودة: `app_state.dart` و`home_shell.dart` سيقسمان إلى Controllers وRepositories وشاشات لكل Feature في المراحل التالية.

## 5. تدفق تسجيل الدخول

1. `ApiClient` يقرأ عنوان API والـToken المحفوظ.
2. `AuthRepository` ينفذ طلبات Login وLogout واستعادة الجلسة.
3. `AuthController` يدير حالات Loading وError وAuthenticated.
4. `AuthGate` يعرض Splash أو Login أو Home حسب الحالة.
5. الـToken يحفظ في Secure Storage ويرسل كـBearer Token.
6. API يعيد الصلاحيات، والواجهة تعرض العمليات المسموحة فقط.

الحساب الذي عليه `ForcePasswordChange` لا يمكنه دخول الموبايل قبل تغيير كلمة المرور من برنامج الكمبيوتر.

## 6. الاتصال بالكمبيوتر

شغل API من جذر المشروع:

```powershell
$savedConnection = [Environment]::GetEnvironmentVariable(
    "HARDWARE_PAINT_SHOP_CONNECTION_STRING",
    "User"
)

$env:HARDWARE_PAINT_SHOP_CONNECTION_STRING = $savedConnection

dotnet run --project .\src\HardwarePaintShop.Api\HardwarePaintShop.Api.csproj
```

اعرف IPv4 للكمبيوتر:

```powershell
ipconfig
```

مثال عنوان داخل التطبيق:

```text
http://192.168.1.7:5000
```

- لا تستخدم `localhost` على هاتف حقيقي.
- الهاتف والكمبيوتر يجب أن يكونا على نفس Wi-Fi.
- أوقف VPN وبيانات الهاتف عند اختبار الشبكة المحلية.
- اختبر أولًا من متصفح الهاتف: `http://IP:5000/api/health`.

## 7. Firewall أثناء التطوير

من PowerShell كمسؤول:

```powershell
New-NetFirewallRule `
  -DisplayName "Hardware Paint Shop Mobile API" `
  -Direction Inbound `
  -Action Allow `
  -Protocol TCP `
  -LocalPort 5000 `
  -Profile Private
```

القاعدة للشبكات الخاصة فقط. لا تفتح المنفذ على شبكة Public أو الإنترنت.

## 8. تجهيز المشروع لأول مرة

من مجلد التطبيق:

```powershell
cd .\mobile\HardwarePaintShopMobile
flutter create --platforms=android --org com.hardwarepaintshop .
flutter pub get
flutter analyze
flutter test
```

تشغيل على هاتف متصل بـUSB:

```powershell
flutter devices
flutter run
```

## 9. إخراج APK

للاختبار:

```powershell
flutter build apk --debug
```

المسار:

```text
build\app\outputs\flutter-apk\app-debug.apk
```

للإصدار النهائي:

```powershell
flutter build apk --release
```

يجب إعداد Android signing key قبل توزيع نسخة Release على العملاء.

## 10. Offline والمزامنة

التطبيق يحتفظ بكاش محلي ويستطيع وضع بعض عمليات الكتابة في Pending Queue عند تعذر الوصول للكمبيوتر. كل عملية لها `operationId` فريد، وAPI يستخدمه لمنع تنفيذ نفس العملية مرتين.

التسلسل:

```text
عملية على الهاتف
→ محاولة إرسال مباشر
→ عند فشل الشبكة تحفظ في SQLite
→ المستخدم يضغط مزامنة
→ رفع العمليات المعلقة
→ تنزيل آخر تغييرات المنتجات والعملاء والتنبيهات
```

حدود النسخة الحالية:

- بعض Lookups والتفاصيل الكاملة لا تزال تحتاج اتصالًا مباشرًا.
- Dashboard وكشوف الحساب لا يعملان بالكامل من الكاش.
- معالجة الحذف والتعطيل والأسعار داخل Delta Sync تحتاج استكمالًا.

## 11. الأمان

- Token محفوظ في `flutter_secure_storage`.
- كل Endpoint يفحص صلاحية المستخدم على السيرفر.
- إخفاء الزر في الواجهة ليس بديلًا عن فحص السيرفر.
- HTTP مسموح حاليًا للشبكة المحلية فقط أثناء الاختبار.
- لا تستخدم Port Forwarding مباشر للمنفذ 5000.
- للوصول خارج المحل استخدم VPN موثوقًا أو HTTPS Reverse Proxy بعد مراجعة أمنية.

## 12. الاختبارات والجودة

```powershell
flutter analyze
flutter test
flutter build apk --debug
```

GitHub Actions معد لتشغيل Analyze وTests وبناء APK Debug عند رفع فرع الموبايل.

## 13. المشاكل الشائعة

### تعذر الوصول إلى اللاب

- تأكد أن نافذة API مفتوحة وتظهر `Now listening`.
- اختبر `/api/health` محليًا ومن الهاتف.
- راجع IP وFirewall ونوع الشبكة Private.

### غيّر كلمة المرور من الكمبيوتر أولًا

- سجل الدخول في الديسكتوب بـ`admin`.
- افتح المستخدمين وعدّل الحساب وأدخل كلمة جديدة ثم احفظ.

### بيانات الدخول غير صحيحة

- استخدم كلمة المرور الجديدة، وليس `admin123` بعد تغييرها.
- تأكد أن الحساب نشط وله صلاحية `Api.Access`.

### التطبيق يعمل لكن البيانات فارغة

- تأكد من صلاحيات الحساب.
- نفذ مزامنة يدوية.
- راجع Log الـAPI وأخطاء التطبيق.

## 14. خطة التطوير التالية

1. فصل Dashboard إلى Repository وController وشاشة مستقلة.
2. نقل Products بالكامل إلى Feature مستقلة.
3. نقل Customers ثم Alerts وSync.
4. إكمال Design System والResponsive UI والـAccessibility.
5. تقوية Offline cache وعمليات الحذف والتعارض.
6. زيادة اختبارات Controllers وRepositories والمزامنة.

