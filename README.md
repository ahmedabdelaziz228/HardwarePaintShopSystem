# HardwarePaintShopSystem — Version 1.1 Mobile RC1

.NET 8 WPF shop-management system backed by PostgreSQL and organized as Domain, Application, Infrastructure, Desktop, API, and test projects.

## Arabic project documentation

- [Desktop application guide](docs/DESKTOP_GUIDE_AR.md)
- [Flutter mobile application guide](mobile/HardwarePaintShopMobile/MOBILE_APP_GUIDE_AR.md)
- [System idea and desktop/mobile integration](docs/SYSTEM_OVERVIEW_AR.md)

## Implemented modules

- Login, users, roles, granular permissions, and immutable audit records.
- Categories, units, price groups, expense categories, products, barcodes, images, unit conversions, prices, costs, and serial tracking.
- Customers and suppliers with credit/payable balances and controlled soft deactivation.
- Smart purchase invoices: draft, posting, landed cost, weighted cost, stock-in, serials, supplier ledger, payments, and controlled void.
- Barcode-first Sales POS: cash/credit/partial payments, editable actual price, minimum-price enforcement, automatic debtor-customer creation, stock-out, serial reservation, and negative-stock prevention.
- Sales and purchase returns linked to an original posted invoice, with quantity limits, serial validation, stock reversal, balance reversal, or cash refund.
- Multiple cashboxes, expenses, customer collections, supplier payments, owner deposits/withdrawals, and cash transfers.
- Live stock balances, stock card, physical count, transactional inventory adjustment, and historical count log.
- Low-stock, negative-stock, and overdue-invoice alerts.
- Financial dashboard and reports for sales, purchases, returns, expenses, historical captured cost, net profit, stock value, customer debt, supplier debt, daily sales, and product profitability.
- Customer account statements plus detailed A4/58/80 mm invoices that can be printed or saved through Microsoft Print to PDF.
- Shop, legal invoice fields, logo, paper size, footer, printer, and backup settings stored in the database.
- PostgreSQL custom-format backup, automatic daily backup, retention cleanup, backup history, safety backup before restore, and restore.
- First-run/recovery database connection screen. The app does not open the login screen when database initialization fails.
- 30-day trial and RSA-signed offline licenses bound to the Windows machine.
- Self-contained `win-x64` publish script and Inno Setup installer definition.
- Authenticated local API with device sessions and permission checks for products, customers, collections, stock adjustments, alerts, and synchronization.
- Arabic Flutter mobile client with dashboard, barcode scanning, product/customer workflows, alerts, camera product images, and an offline operation queue.

The sales profitability migration stores cost/profit and customer-balance snapshots on posted invoices. Migrations, standard units, the default retail price group, new permissions, and Owner assignments are applied or seeded automatically on startup.

## Build and test on Windows

Run from the project root. Always name the solution because the folder intentionally contains both `.sln` and `.slnx` files:

```powershell
dotnet restore .\HardwarePaintShopSystem.sln
dotnet build .\HardwarePaintShopSystem.sln -c Debug --no-restore
dotnet test .\HardwarePaintShopSystem.sln -c Debug --no-build
```

Start the desktop app:

```powershell
dotnet run --project .\src\HardwarePaintShop.Desktop\HardwarePaintShop.Desktop.csproj
```

Start the API during development in a second PowerShell window:

```powershell
dotnet run --project .\src\HardwarePaintShop.Api\HardwarePaintShop.Api.csproj
Invoke-RestMethod http://localhost:5000/api/health
```

The Owner role is seeded with mobile/API permissions. Change the initial admin password in the desktop app before signing in from mobile. For a physical phone, use the computer's private IPv4 address, for example `http://192.168.1.20:5000`, and keep both devices on the same trusted network.

## Flutter mobile app

Install Flutter stable, then run from `mobile\HardwarePaintShopMobile`:

```powershell
flutter create --platforms=android --org com.hardwarepaintshop .
flutter pub get
flutter analyze
flutter test
flutter run
```

Create the Android package with:

```powershell
flutter build apk --release
```

The APK is produced under `build\app\outputs\flutter-apk\app-release.apk`. See the mobile folder README for phone and emulator API addresses.

On the first connection failure, the desktop setup window asks for PostgreSQL host, port, database, username, and password. The verified connection is saved in the current Windows user's `HARDWARE_PAINT_SHOP_CONNECTION_STRING` environment variable.

Development default login on a new database:

- Username: `admin`
- Password: `admin123`
- Password change is forced.

## Publish and installer

Create and verify a self-contained release:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1
```

Build the installer too (requires Inno Setup 6):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1 -BuildInstaller
```

Outputs:

- `artifacts\publish\win-x64\HardwarePaintShop.Desktop.exe`
- `artifacts\publish\win-x64\Api\HardwarePaintShop.Api.exe`
- `artifacts\installer\HardwarePaintShopSystem_Setup_1.1.0-rc1_win-x64.exe`

PostgreSQL 14+ is a prerequisite. Backup tools are discovered through `PG_BIN`, the normal `PATH`, or common `C:\Program Files\PostgreSQL\<version>\bin` installations.

## Offline licensing

The customer's activation screen displays a machine ID. On the seller machine, generate a permanent key:

```powershell
dotnet run --project .\tools\LicenseGenerator -- "Customer Name" MACHINE-ID permanent Retail
```

Or an expiring key:

```powershell
dotnet run --project .\tools\LicenseGenerator -- "Customer Name" MACHINE-ID 2027-12-31 Retail
```

The private key under `seller-tools` is seller-only. Never include that folder in a customer delivery, installer, public repository, or support attachment. Only the public verification key is embedded in the app.

## Before a paid customer deployment

RC1 is feature-complete for acceptance testing, but do not call it production-ready until all of these pass on the target Windows/PostgreSQL environment:

1. Release build and all automated tests.
2. Fresh-database initialization and forced password change.
3. Full purchase → sale → return → inventory-count lifecycle with real barcodes and serials.
4. Cash, credit, partial payment, collection, supplier payment, expense, and cash-transfer reconciliation.
5. Thermal printer and A4/report printer checks.
6. Backup creation, restore into a disposable database, and record-count comparison.
7. Trial expiry and signed activation on a second Windows account.
8. Installer install, upgrade, desktop shortcut, and uninstall.
9. Mobile login, LAN firewall, barcode camera, offline queue, duplicate-operation protection, and reconnect synchronization on at least two Android devices.
10. Security review, private-key isolation, PostgreSQL backup policy, and customer-specific tax/legal invoice requirements.

The mobile/API layer is an acceptance-test release. Do not expose port 5000 directly to the public internet; remote access should use a reviewed VPN or an HTTPS reverse proxy with appropriate operational security.
