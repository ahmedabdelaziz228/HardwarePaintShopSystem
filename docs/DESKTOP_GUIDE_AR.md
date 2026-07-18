# دليل برنامج الكمبيوتر — Hardware Paint Shop Desktop

## 1. ما هو البرنامج؟

برنامج Windows لإدارة محل حدايد وبويات من نقطة واحدة. البرنامج مبني باستخدام WPF و.NET 8، ويستخدم PostgreSQL كقاعدة بيانات رئيسية. نسخة الكمبيوتر هي المصدر الأساسي للبيانات والعمليات المالية والمخزنية، ومنها تتم إدارة المستخدمين والصلاحيات والإعدادات والنسخ الاحتياطي.

## 2. الوظائف الرئيسية

- تسجيل الدخول وإدارة المستخدمين والأدوار والصلاحيات.
- إدارة التصنيفات والوحدات وفئات الأسعار وأنواع المصروفات.
- إدارة المنتجات والأكواد والباركود والصور والوحدات والأسعار والسيريالات.
- إدارة العملاء والموردين والأرصدة وحدود الائتمان.
- فواتير المشتريات وإضافة الكميات إلى المخزون وحساب التكلفة.
- نقطة بيع سريعة تدعم الباركود والبيع النقدي والآجل والدفع الجزئي.
- مرتجعات المبيعات والمشتريات وربطها بالفواتير الأصلية.
- الخزائن والتحصيلات والمدفوعات والمصروفات والتحويلات المالية.
- حركة المخزون وكارت الصنف والجرد والتسويات والتنبيهات.
- لوحة معلومات وتقارير مالية ومخزنية.
- طباعة الإيصالات والتقارير وتصدير CSV.
- النسخ الاحتياطي والاستعادة وإعدادات المحل والطابعة.
- ترخيص تجريبي وتفعيل Offline مرتبط بجهاز العميل.
- تشغيل API محلي لخدمة تطبيق الموبايل في النسخة المنشورة.

## 3. التقنيات المستخدمة

| الجزء | التقنية |
| --- | --- |
| واجهة الكمبيوتر | WPF / XAML |
| لغة البرمجة | C# / .NET 8 |
| نمط الواجهة | MVVM باستخدام CommunityToolkit.Mvvm |
| قاعدة البيانات | PostgreSQL |
| الوصول للبيانات | Entity Framework Core + Npgsql |
| التسجيل | Serilog |
| كلمات المرور | BCrypt |
| الاختبارات | xUnit |
| التثبيت | Self-contained publish + Inno Setup |

## 4. طبقات المشروع

```text
src/
├── HardwarePaintShop.Domain
│   └── الكيانات والقيم والتعدادات الأساسية
├── HardwarePaintShop.Application
│   └── العقود والنماذج والصلاحيات والمنطق المشترك
├── HardwarePaintShop.Infrastructure
│   └── PostgreSQL وEF Core والخدمات والمستودعات
├── HardwarePaintShop.Desktop
│   └── شاشات WPF وViewModels والتنسيقات والتنقل
└── HardwarePaintShop.Api
    └── API محلي للموبايل والمزامنة
```

اتجاه الاعتماد المقصود:

```text
Desktop → Application ← Infrastructure → PostgreSQL
              ↑
            Domain
```

## 5. دورة تشغيل البرنامج

1. قراءة `appsettings.json` ومتغيرات البيئة.
2. تهيئة التسجيل في ملفات Log.
3. اختبار الاتصال بـPostgreSQL وتطبيق التهيئة المطلوبة.
4. عرض شاشة إعداد قاعدة البيانات عند فشل الاتصال.
5. فحص الفترة التجريبية أو الترخيص.
6. تشغيل النسخ الاحتياطي التلقائي عند استحقاقه.
7. عرض شاشة تسجيل الدخول.
8. تحميل صلاحيات المستخدم وبناء القائمة الجانبية المناسبة.
9. في النسخة المنشورة، تشغيل API المحلي تلقائيًا إذا كان موجودًا.

## 6. قاعدة البيانات

Connection String الافتراضي موجود في ملفات الإعداد كقيمة تجريبية فقط. شاشة إعداد قاعدة البيانات تحفظ الاتصال الصحيح في متغير مستخدم Windows:

```text
HARDWARE_PAINT_SHOP_CONNECTION_STRING
```

صيغة نموذجية:

```text
Host=localhost;Port=5432;Database=hardware_paint_shop;Username=postgres;Password=YOUR_PASSWORD
```

لا تضع كلمة مرور قاعدة بيانات حقيقية في GitHub.

## 7. أول تشغيل

في قاعدة بيانات جديدة يتم إنشاء حساب الإدارة:

```text
Username: admin
Password: admin123
```

يجب تغيير كلمة المرور الافتراضية فورًا:

1. سجل الدخول من برنامج الكمبيوتر.
2. افتح شاشة **المستخدمين**.
3. اضغط تعديل أمام `admin`.
4. أدخل كلمة مرور جديدة واترك الدور `Owner` والحساب نشطًا.
5. اضغط **حفظ**.

تغيير كلمة المرور يلغي علامة `ForcePasswordChange` ويسمح للحساب باستخدام تطبيق الموبايل.

## 8. البناء والتشغيل

من جذر المشروع:

```powershell
dotnet restore .\HardwarePaintShopSystem.sln
dotnet build .\HardwarePaintShopSystem.sln -c Debug --no-restore
dotnet test .\HardwarePaintShopSystem.sln -c Debug --no-build
dotnet run --project .\src\HardwarePaintShop.Desktop\HardwarePaintShop.Desktop.csproj
```

المتطلبات:

- Windows 10 أو 11 x64.
- .NET 8 SDK للتطوير.
- PostgreSQL 14 أو أحدث.
- أدوات PostgreSQL `pg_dump` و`pg_restore` للنسخ والاستعادة.

## 9. تشغيل API يدويًا أثناء التطوير

```powershell
$savedConnection = [Environment]::GetEnvironmentVariable(
    "HARDWARE_PAINT_SHOP_CONNECTION_STRING",
    "User"
)

$env:HARDWARE_PAINT_SHOP_CONNECTION_STRING = $savedConnection

dotnet run --project .\src\HardwarePaintShop.Api\HardwarePaintShop.Api.csproj
```

الاختبار:

```powershell
Invoke-RestMethod http://localhost:5000/api/health
```

## 10. النشر وInstaller

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1
```

ولإنشاء Installer بعد تثبيت Inno Setup 6:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1 -BuildInstaller
```

النشر ينتج برنامج Windows وAPI محليًا داخل مجلد `Api`، والـInstaller ينشئ الاختصارات وقاعدة Firewall للشبكات الخاصة.

## 11. الترخيص والأمان

- المفتاح العام فقط مدمج داخل البرنامج.
- المفتاح الخاص موجود على جهاز البائع Offline فقط.
- لا ترفع `license-private-key.pem` إلى Git أو نسخة العميل.
- أي مفتاح خاص تم نشره سابقًا يجب اعتباره ملغيًا.
- لا تعرض PostgreSQL أو منفذ API مباشرة على الإنترنت.
- استخدم كلمات مرور قوية وحسابات بصلاحيات مناسبة.

## 12. المخزن الرئيسي

النظام يعمل حاليًا بمخزن رئيسي واحد. الرصيد لا يُكتب داخل سجل المنتج، بل يُحسب دائمًا من مجموع حركات المخزون، ولذلك لا يجوز تعديل الكمية مباشرة من قاعدة البيانات.

- إنشاء منتج جديد يمكن أن يسجل رصيدًا افتتاحيًا وتكلفة متوسطة أولى داخل معاملة واحدة.
- المنتجات المتتبعة بالسيريال تُنشأ برصيد صفر، ثم تدخل كمياتها مع أرقام السيريال من دورة الشراء.
- إضافة أو صرف أو تسوية الرصيد تحتاج صلاحية `Inventory.Adjust` وسببًا واضحًا.
- النظام يمنع أي عملية صرف تجعل الرصيد سالبًا.
- كل تعديل يدوي يُحفظ في كارت حركة الصنف وسجل المراجعة باسم المستخدم.
- الجرد الفعلي ينشئ حركة فرق فقط، ولا يستبدل تاريخ الحركات السابقة.

## 13. الملفات المهمة للمطور

```text
src/HardwarePaintShop.Desktop/App.xaml.cs
src/HardwarePaintShop.Desktop/DesktopServiceRegistration.cs
src/HardwarePaintShop.Desktop/ViewModels/
src/HardwarePaintShop.Desktop/Views/
src/HardwarePaintShop.Desktop/Styles/
src/HardwarePaintShop.Infrastructure/Data/AppDbContext.cs
src/HardwarePaintShop.Infrastructure/Data/DatabaseInitializer.cs
src/HardwarePaintShop.Application/Security/PermissionCodes.cs
```

## 14. التحقق قبل البيع

- تجربة دورة شراء وبيع ومرتجع وجرد كاملة.
- مطابقة النقدية والأرصدة والمخزون مع التقارير.
- اختبار قارئ الباركود والطابعة الحرارية.
- إنشاء Backup واستعادته في قاعدة اختبار.
- اختبار الترخيص على جهاز آخر.
- اختبار Installer والتحديث وإلغاء التثبيت.
- مراجعة المتطلبات الضريبية والقانونية الخاصة بالعميل.
