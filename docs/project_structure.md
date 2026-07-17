# Hardware & Paint Shop Management System — Project Structure

> WPF Desktop App · .NET 8 · MVVM · PostgreSQL / EF Core

---

## Solution Layout

```
HardwarePaintShopSystem.sln
│
├── src/
│   ├── HardwarePaintShop.Desktop/       ← WPF app (startup project)
│   ├── HardwarePaintShop.Application/   ← Business logic (Services, DTOs, Interfaces)
│   ├── HardwarePaintShop.Domain/        ← EF Core Entities + Domain rules
│   ├── HardwarePaintShop.Infrastructure/← EF Core DbContext, Repos, external services
│   └── HardwarePaintShop.Api/           ← ASP.NET Core Web API (Phase 6)
│
├── tests/
│   ├── HardwarePaintShop.UnitTests/
│   └── HardwarePaintShop.IntegrationTests/
│
├── database/
│   ├── phase_01_database_schema.sql
│   ├── phase_01_supplemental.sql        ← Schema fixes from issue list
│   └── seeds/
│       ├── seed_permissions.sql
│       └── seed_default_user.sql
│
└── docs/
    ├── shop_management_system_plan.md
    ├── implementation_plan.md
    └── project_structure.md             ← This file
```

---

## Layer Responsibilities

| Layer | Project | Contains |
|---|---|---|
| **Presentation** | `Desktop` | Views (XAML), ViewModels, Converters, Controls |
| **Application** | `Application` | Service interfaces & implementations, DTOs, Mapping |
| **Domain** | `Domain` | EF Core entity classes, enums, domain constants |
| **Infrastructure** | `Infrastructure` | DbContext, Migrations, Repositories, Printing, Backup |
| **API** | `Api` | Controllers, JWT Auth, API-specific DTOs |

---

## Detailed Folder Structure

### `HardwarePaintShop.Desktop/` — WPF App

```
HardwarePaintShop.Desktop/
│
├── App.xaml                          ← Global resources, startup, DI wiring
├── App.xaml.cs
├── appsettings.json                  ← DB connection, app settings
│
├── Assets/
│   ├── Images/                       ← App icons, logos, default product image
│   └── Fonts/                        ← Optional custom fonts
│
├── Styles/
│   ├── App.Styles.xaml               ← Global merged dictionary
│   ├── Buttons.xaml
│   ├── DataGrids.xaml
│   ├── Forms.xaml
│   └── Colors.xaml                   ← Brand palette
│
├── Controls/                         ← Reusable custom WPF controls
│   ├── BarcodeTextBox.xaml           ← TextBox that captures scanner input
│   ├── NumericUpDown.xaml
│   ├── ProductSearchBox.xaml
│   ├── AlertBadge.xaml
│   └── InvoiceItemsGrid.xaml
│
├── Converters/                       ← IValueConverter implementations
│   ├── BoolToVisibilityConverter.cs
│   ├── DecimalFormatConverter.cs
│   ├── DateFormatConverter.cs
│   ├── StockQtyDisplayConverter.cs   ← Converts base-unit qty → "47 شكارة و20 كيلو"
│   └── NullImageConverter.cs
│
├── Views/
│   ├── Shell/
│   │   ├── MainWindow.xaml           ← Navigation shell (sidebar + frame)
│   │   └── LoginWindow.xaml
│   │
│   ├── Dashboard/
│   │   └── DashboardView.xaml
│   │
│   ├── Products/
│   │   ├── ProductListView.xaml
│   │   ├── ProductEditView.xaml
│   │   ├── ProductUnitsTab.xaml
│   │   ├── ProductPricesTab.xaml
│   │   ├── ProductBarcodesTab.xaml
│   │   ├── ProductSerialsTab.xaml
│   │   └── PriceInquiryView.xaml
│   │
│   ├── Customers/
│   │   ├── CustomerListView.xaml
│   │   ├── CustomerEditView.xaml
│   │   └── CustomerAccountView.xaml
│   │
│   ├── Suppliers/
│   │   ├── SupplierListView.xaml
│   │   ├── SupplierEditView.xaml
│   │   └── SupplierAccountView.xaml
│   │
│   ├── Sales/
│   │   ├── SalesInvoiceListView.xaml
│   │   └── SalesInvoiceEditView.xaml ← POS-style invoice screen
│   │
│   ├── Purchases/
│   │   ├── PurchaseInvoiceListView.xaml
│   │   └── PurchaseInvoiceEditView.xaml
│   │
│   ├── Returns/
│   │   ├── ReturnListView.xaml
│   │   ├── SalesReturnView.xaml
│   │   └── PurchaseReturnView.xaml
│   │
│   ├── Inventory/
│   │   ├── StockReportView.xaml
│   │   ├── StockMovementView.xaml
│   │   └── InventoryCountView.xaml
│   │
│   ├── Cashbox/
│   │   ├── CashboxView.xaml
│   │   ├── ExpenseListView.xaml
│   │   ├── ExpenseEditView.xaml
│   │   └── OwnerWithdrawalView.xaml
│   │
│   ├── Reports/
│   │   ├── SalesReportView.xaml
│   │   ├── PurchaseReportView.xaml
│   │   ├── CustomerStatementView.xaml
│   │   ├── SupplierStatementView.xaml
│   │   ├── StockBalanceReportView.xaml
│   │   └── CashboxReportView.xaml
│   │
│   ├── Alerts/
│   │   └── AlertsView.xaml
│   │
│   ├── Settings/
│   │   ├── SettingsView.xaml
│   │   ├── UsersView.xaml
│   │   ├── RolesView.xaml
│   │   ├── BackupView.xaml
│   │   └── Lookups/
│   │       ├── CategoriesView.xaml
│   │       ├── UnitsView.xaml
│   │       ├── PriceGroupsView.xaml
│   │       └── ExpenseCategoriesView.xaml
│   │
│   └── Shared/
│       ├── ConfirmDialog.xaml
│       └── LoadingOverlay.xaml
│
└── ViewModels/
    ├── Shell/
    │   ├── MainViewModel.cs          ← Navigation, current user, alert badge
    │   └── LoginViewModel.cs
    │
    ├── Dashboard/
    │   └── DashboardViewModel.cs
    │
    ├── Products/
    │   ├── ProductListViewModel.cs
    │   ├── ProductEditViewModel.cs
    │   ├── ProductUnitsViewModel.cs
    │   ├── ProductPricesViewModel.cs
    │   ├── ProductBarcodesViewModel.cs
    │   ├── ProductSerialsViewModel.cs
    │   └── PriceInquiryViewModel.cs
    │
    ├── Customers/
    │   ├── CustomerListViewModel.cs
    │   ├── CustomerEditViewModel.cs
    │   └── CustomerAccountViewModel.cs
    │
    ├── Suppliers/
    │   ├── SupplierListViewModel.cs
    │   ├── SupplierEditViewModel.cs
    │   └── SupplierAccountViewModel.cs
    │
    ├── Sales/
    │   ├── SalesInvoiceListViewModel.cs
    │   └── SalesInvoiceEditViewModel.cs
    │
    ├── Purchases/
    │   ├── PurchaseInvoiceListViewModel.cs
    │   └── PurchaseInvoiceEditViewModel.cs
    │
    ├── Returns/
    │   ├── ReturnListViewModel.cs
    │   ├── SalesReturnViewModel.cs
    │   └── PurchaseReturnViewModel.cs
    │
    ├── Inventory/
    │   ├── StockReportViewModel.cs
    │   ├── StockMovementViewModel.cs
    │   └── InventoryCountViewModel.cs
    │
    ├── Cashbox/
    │   ├── CashboxViewModel.cs
    │   ├── ExpenseListViewModel.cs
    │   ├── ExpenseEditViewModel.cs
    │   └── OwnerWithdrawalViewModel.cs
    │
    ├── Reports/
    │   ├── SalesReportViewModel.cs
    │   ├── CustomerStatementViewModel.cs
    │   ├── SupplierStatementViewModel.cs
    │   ├── StockBalanceReportViewModel.cs
    │   └── CashboxReportViewModel.cs
    │
    ├── Alerts/
    │   └── AlertsViewModel.cs
    │
    └── Settings/
        ├── SettingsViewModel.cs
        ├── UsersViewModel.cs
        ├── RolesViewModel.cs
        └── BackupViewModel.cs
```

---

### `HardwarePaintShop.Domain/` — Entities & Enums

```
HardwarePaintShop.Domain/
│
├── Entities/
│   ├── Category.cs
│   ├── Unit.cs
│   ├── PriceGroup.cs
│   ├── ExpenseCategory.cs
│   ├── Role.cs
│   ├── Permission.cs
│   ├── RolePermission.cs
│   ├── User.cs
│   ├── Customer.cs
│   ├── Supplier.cs
│   ├── Product.cs
│   ├── ProductUnit.cs
│   ├── ProductPrice.cs
│   ├── ProductCost.cs
│   ├── ProductBarcode.cs
│   ├── ProductSerial.cs
│   ├── Cashbox.cs
│   ├── CashMovement.cs
│   ├── Expense.cs
│   ├── OwnerWithdrawal.cs
│   ├── SalesInvoice.cs
│   ├── SalesInvoiceItem.cs
│   ├── PurchaseInvoice.cs
│   ├── PurchaseInvoiceItem.cs
│   ├── InvoicePayment.cs
│   ├── CustomerTransaction.cs
│   ├── SupplierTransaction.cs
│   ├── StockMovement.cs
│   ├── Return.cs
│   ├── ReturnItem.cs
│   ├── InventoryCount.cs
│   ├── InventoryCountItem.cs
│   ├── Alert.cs
│   ├── BackupLog.cs
│   ├── SyncDevice.cs
│   ├── SyncLog.cs
│   ├── AppSetting.cs
│   └── AuditLog.cs
│
└── Enums/
    ├── PaymentStatus.cs       ← paid, partial, unpaid
    ├── InvoiceStatus.cs       ← active, void, returned
    ├── SerialStatus.cs        ← available, sold, returned, damaged, reserved
    ├── MovementType.cs        ← purchase, sale, sales_return, purchase_return, adjustment, damaged
    ├── CashDirection.cs       ← in, out
    ├── AlertSeverity.cs       ← info, warning, critical
    ├── ReturnType.cs          ← sales_return, purchase_return
    └── SyncDirection.cs       ← upload, download
```

---

### `HardwarePaintShop.Application/` — Services & DTOs

```
HardwarePaintShop.Application/
│
├── Interfaces/
│   ├── IProductService.cs
│   ├── ISalesInvoiceService.cs
│   ├── IPurchaseInvoiceService.cs
│   ├── IStockMovementService.cs
│   ├── ICashMovementService.cs
│   ├── ICustomerService.cs
│   ├── ISupplierService.cs
│   ├── IReturnService.cs
│   ├── IInventoryCountService.cs
│   ├── IExpenseService.cs
│   ├── IAlertService.cs
│   ├── IBackupService.cs
│   ├── IReportService.cs
│   ├── IAuthService.cs
│   ├── IPermissionService.cs
│   ├── IAuditService.cs
│   └── ISettingsService.cs
│
├── Services/
│   ├── ProductService.cs
│   ├── SalesInvoiceService.cs       ← Creates invoice + stock movements + cash movements atomically
│   ├── PurchaseInvoiceService.cs
│   ├── StockMovementService.cs      ← Single entry-point for all inventory changes
│   ├── CashMovementService.cs       ← Single entry-point for all cashbox changes
│   ├── CustomerService.cs
│   ├── SupplierService.cs
│   ├── ReturnService.cs
│   ├── InventoryCountService.cs
│   ├── ExpenseService.cs
│   ├── AlertGeneratorService.cs     ← Background checks + insert alerts
│   ├── BackupService.cs             ← pg_dump + restore + scheduling
│   ├── ReportService.cs
│   ├── AuthService.cs
│   ├── PermissionService.cs
│   ├── AuditService.cs
│   └── SettingsService.cs
│
├── DTOs/
│   ├── Products/
│   │   ├── ProductDto.cs
│   │   ├── ProductSummaryDto.cs
│   │   ├── ProductUnitDto.cs
│   │   ├── ProductPriceDto.cs
│   │   └── PriceInquiryDto.cs
│   ├── Invoices/
│   │   ├── SalesInvoiceDto.cs
│   │   ├── SalesInvoiceItemDto.cs
│   │   ├── PurchaseInvoiceDto.cs
│   │   └── PurchaseInvoiceItemDto.cs
│   ├── Customers/
│   │   ├── CustomerDto.cs
│   │   └── CustomerStatementDto.cs
│   ├── Suppliers/
│   │   ├── SupplierDto.cs
│   │   └── SupplierStatementDto.cs
│   ├── Reports/
│   │   ├── SalesReportDto.cs
│   │   ├── StockBalanceDto.cs
│   │   └── CashboxReportDto.cs
│   └── Dashboard/
│       └── DashboardSummaryDto.cs
│
└── Helpers/
    ├── UnitConverter.cs             ← Converts qty between any unit and base unit
    ├── StockDisplayFormatter.cs     ← Formats base-unit qty → "47 شكارة و20 كيلو"
    └── InvoiceNumberGenerator.cs    ← Generates sequential invoice numbers
```

---

### `HardwarePaintShop.Infrastructure/` — Data Access & External

```
HardwarePaintShop.Infrastructure/
│
├── Data/
│   ├── AppDbContext.cs              ← EF Core DbContext, all DbSets
│   ├── AppDbContextFactory.cs       ← For EF migrations CLI
│   └── Configurations/              ← IEntityTypeConfiguration per entity
│       ├── ProductConfiguration.cs
│       ├── SalesInvoiceConfiguration.cs
│       └── ... (one per entity)
│
├── Migrations/                      ← EF Core auto-generated migrations
│
├── Repositories/                    ← Optional repo pattern (thin wrappers)
│   ├── IRepository.cs
│   └── Repository.cs
│
├── Printing/
│   ├── IThermalPrinter.cs
│   ├── ThermalPrinterService.cs     ← ESC/POS commands via ESCPOS.NET
│   ├── IReportPrinter.cs
│   └── WpfReportPrinter.cs          ← FlowDocument → PrintDialog
│
├── Backup/
│   ├── PgDumpBackupService.cs       ← Runs pg_dump.exe as Process
│   └── BackupScheduler.cs           ← Timer-based daily backup trigger
│
└── ProductImages/
    └── ImageStorageService.cs       ← Save/Load product images from local path
```

---

### `HardwarePaintShop.Api/` — ASP.NET Core Web API (Phase 6)

```
HardwarePaintShop.Api/
│
├── Controllers/
│   ├── AuthController.cs
│   ├── ProductsController.cs
│   ├── CustomersController.cs
│   ├── StockController.cs
│   ├── AlertsController.cs
│   ├── PaymentsController.cs
│   └── SyncController.cs
│
├── Middleware/
│   ├── PermissionMiddleware.cs
│   └── ExceptionHandlerMiddleware.cs
│
├── DTOs/                            ← API-specific request/response DTOs
│
└── Program.cs
```

---

## Key NuGet Packages

| Package | Purpose |
|---|---|
| `Microsoft.EntityFrameworkCore` 8.x | ORM |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL provider |
| `CommunityToolkit.Mvvm` | MVVM source generators |
| `MahApps.Metro` | WPF modern theming |
| `MaterialDesignThemes` | Material icons + controls (alternative) |
| `Microsoft.Extensions.DependencyInjection` | DI container |
| `Microsoft.Extensions.Configuration.Json` | appsettings.json |
| `Serilog.Sinks.File` + `Serilog.Sinks.PostgreSQL` | Logging |
| `ZXing.Net.Bindings.Windows.Compatibility` | Barcode generation |
| `ESCPOS.NET` | ESC/POS thermal printing |
| `BCrypt.Net-Next` | Password hashing |
| `AutoMapper` | Entity ↔ DTO mapping |

---

## Navigation Flow

```
LoginWindow
    └── MainWindow (shell with sidebar nav)
            ├── Dashboard
            ├── Products
            │     ├── Product List
            │     ├── Add / Edit Product
            │     └── Price Inquiry
            ├── Sales Invoices
            │     ├── Invoice List
            │     └── New Invoice (POS screen)
            ├── Purchase Invoices
            ├── Customers
            │     └── Customer Account (per customer)
            ├── Suppliers
            │     └── Supplier Account (per supplier)
            ├── Returns
            ├── Inventory
            │     ├── Stock Report
            │     └── Inventory Count
            ├── Cashbox
            ├── Expenses
            ├── Reports
            ├── Alerts
            └── Settings
                  ├── Users & Roles
                  ├── Lookups
                  └── Backup & Restore
```

---

## Data Flow Rules (Summary)

```
Any stock change   ──► StockMovementService.Record()   ──► stock_movements row
Any cashbox change ──► CashMovementService.Record()    ──► cash_movements row + update cashboxes.current_balance
Any AR/AP change   ──► Customer/SupplierTransactionService ──► customer/supplier_transactions row
Any sensitive edit ──► AuditService.Log()              ──► audit_logs row
Invoice void       ──► Reversal movements auto-created, all services called in one DB transaction
```
