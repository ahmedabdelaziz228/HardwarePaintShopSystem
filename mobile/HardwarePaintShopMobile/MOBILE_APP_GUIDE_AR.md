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
| إدارة الحالة | flutter_bloc باستخدام Cubit |
| الاتصال | Dio 5.10 عبر ApiClient مركزي |
| التخزين المحلي | SQLite عبر sqflite |
| Token | flutter_secure_storage |
| الإعدادات المحلية | shared_preferences |
| الباركود | mobile_scanner |
| الكاميرا | image_picker |
| التنسيق المالي | intl |

لا توجد استدعاءات API داخل الشاشات، ولا تعتمد الواجهة على `ChangeNotifier`. كل عملية غير متزامنة محاطة بمعالجة أخطاء عند حدودها، ثم تتحول إلى حالات Loading وSuccess وError مفهومة للمستخدم.

## 4. المعمارية الحالية

التطبيق منظم بالكامل بأسلوب Feature-first. يوجد داخل `lib` جذران معماريان فقط: `core` للبنية المشتركة و`features` لوظائف التطبيق. كل Feature تحتوي `data` و`logic` و`ui` حسب احتياجها:

```text
lib/
├── main.dart
├── core/
│   ├── app/
│   ├── config/
│   ├── errors/
│   ├── network/
│   ├── state/
│   ├── storage/
│   ├── theme/
│   ├── utils/
│   └── widgets/
└── features/
    ├── auth/
    ├── dashboard/
    ├── products/
    ├── customers/
    ├── alerts/
    ├── sync/
    └── shell/
        ├── data/   # Models وRepositories عند الحاجة
        ├── logic/  # Cubit وState
        └── ui/     # Pages وWidgets الخاصة بالميزة
```

قواعد الاعتماد:

- `ui` تتعامل مع Cubit فقط ولا تنفذ HTTP أو SQL.
- `logic` يدير الحالات والقرارات ولا يعرف تفاصيل Widgets.
- `data` يحتوي Models وRepositories ويتعامل مع Core network/storage.
- `core` لا يعتمد على Feature معينة.
- الأنماط المشتركة بين أكثر من Feature فقط توضع في `core/widgets`.

## 5. تدفق تسجيل الدخول

1. `ApiClient` المبني على Dio يقرأ عنوان API والـToken المحفوظ.
2. `AuthRepository` ينفذ طلبات Login وLogout واستعادة الجلسة داخل `try/catch`.
3. `AuthCubit` يدير حالات Loading وError وAuthenticated.
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
dart format --output=none --set-exit-if-changed lib test
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

## 14. ديون تقنية متبقية

تمت هجرة Dashboard وProducts وCustomers وAlerts وSync إلى Features مستقلة. البنود المتبقية لا تمنع التشغيل، لكنها أولويات المرحلة التالية:

1. زيادة Unit Tests للـRepositories وحالات فشل الشبكة وSQLite.
2. إضافة اختبارات Cubit تفصيلية لكل انتقال حالة.
3. دعم Delta Sync للحذف والتعطيل وتعارض تعديل نفس السجل.
4. توسيع كاش Dashboard وكشف حساب العميل للعمل بدون اتصال كاملًا.
5. اختبار Accessibility وResponsive layout على أحجام أجهزة حقيقية إضافية.
6. إضافة HTTPS أو VPN موثوق قبل أي وصول من خارج شبكة المحل.
