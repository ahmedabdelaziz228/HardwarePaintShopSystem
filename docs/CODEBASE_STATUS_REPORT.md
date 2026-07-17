# Codebase Status Report

## 1. Executive Summary

> Historical snapshot: this report describes the original foundation and is superseded by `VERSION_1_0_RC1_ACCEPTANCE.md` and the current README. The RC1 desktop now implements the operational shop workflows.

Current stage at the time this historical report was written: early scaffold / foundation stage. The repository contains a .NET 8 solution with Domain, Application, Infrastructure, WPF Desktop, API, UnitTests, and IntegrationTests projects.

The solution is not currently build-verified. `dotnet restore HardwarePaintShopSystem.sln` failed in the sandbox with `NU1301` because NuGet access was blocked; the approved network restore attempt timed out after 124 seconds with no completion output. `dotnet build HardwarePaintShopSystem.sln --no-restore` failed with 7 `NETSDK1004` errors because `project.assets.json` files do not exist.

Most critical current problem: restore/build cannot complete, and static inspection shows likely compile/runtime blockers after restore, including Application services referencing Infrastructure types without an Application-to-Infrastructure project reference, services requiring `IDbContextFactory<AppDbContext>` while DI registers only `AddDbContext<AppDbContext>`, and lookup code using entity properties that do not exist.

## 2. Repository Structure

Solution file: `HardwarePaintShopSystem.sln`

Projects present:

| Project | Path | Target |
|---|---|---|
| HardwarePaintShop.Desktop | `src/HardwarePaintShop.Desktop/HardwarePaintShop.Desktop.csproj` | `net8.0-windows`, WPF WinExe |
| HardwarePaintShop.Domain | `src/HardwarePaintShop.Domain/HardwarePaintShop.Domain.csproj` | `net8.0` |
| HardwarePaintShop.Application | `src/HardwarePaintShop.Application/HardwarePaintShop.Application.csproj` | `net8.0` |
| HardwarePaintShop.Infrastructure | `src/HardwarePaintShop.Infrastructure/HardwarePaintShop.Infrastructure.csproj` | `net8.0` |
| HardwarePaintShop.Api | `src/HardwarePaintShop.Api/HardwarePaintShop.Api.csproj` | `net8.0` |
| HardwarePaintShop.UnitTests | `tests/HardwarePaintShop.UnitTests/HardwarePaintShop.UnitTests.csproj` | `net8.0` |
| HardwarePaintShop.IntegrationTests | `tests/HardwarePaintShop.IntegrationTests/HardwarePaintShop.IntegrationTests.csproj` | `net8.0` |

Project references:

| Project | References |
|---|---|
| Desktop | Application, Domain, Infrastructure |
| Application | Domain |
| Infrastructure | Application, Domain |
| Api | Application, Domain, Infrastructure |
| UnitTests | Application, Domain |
| IntegrationTests | Infrastructure, Domain |

NuGet packages:

| Project | Packages |
|---|---|
| Desktop | CommunityToolkit.Mvvm 8.3.2; Microsoft.Extensions.DependencyInjection 8.0.1; Microsoft.Extensions.Configuration.Json 8.0.1; Serilog 4.1.0; Serilog.Sinks.File 6.0.0; Serilog.Extensions.Hosting 8.0.0 |
| Application | BCrypt.Net-Next 4.0.3; Microsoft.Extensions.DependencyInjection.Abstractions 8.0.2 |
| Infrastructure | Microsoft.EntityFrameworkCore 8.0.11; Microsoft.EntityFrameworkCore.Design 8.0.11; Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11; Microsoft.Extensions.DependencyInjection 8.0.1; Microsoft.Extensions.Configuration.Json 8.0.1; Serilog 4.1.0; Serilog.Sinks.File 6.0.0 |
| UnitTests | Microsoft.NET.Test.Sdk 17.11.1; xunit 2.9.2; xunit.runner.visualstudio 2.8.2; Moq 4.20.72; FluentAssertions 6.12.2 |
| IntegrationTests | Microsoft.NET.Test.Sdk 17.11.1; xunit 2.9.2; xunit.runner.visualstudio 2.8.2; Moq 4.20.72; FluentAssertions 6.12.2; Microsoft.EntityFrameworkCore.InMemory 8.0.11 |

Startup project: documented as `HardwarePaintShop.Desktop` in `docs/project_structure.md`; the `.sln` itself does not encode a startup project.

Architecture currently used: layered solution with WPF MVVM desktop UI, Domain entities/enums, Application interfaces/services/helpers, Infrastructure EF Core DbContext/repository/services, placeholder ASP.NET Core API project, and empty test projects.

Important folders/files:

| Path | Purpose |
|---|---|
| `src/HardwarePaintShop.Domain/Entities` | Domain entity classes grouped by area |
| `src/HardwarePaintShop.Domain/Enums` | Business enum definitions |
| `src/HardwarePaintShop.Application/Interfaces` | Service contracts and request records |
| `src/HardwarePaintShop.Application/Services` | Application service implementations, but they reference Infrastructure |
| `src/HardwarePaintShop.Application/Helpers` | Unit conversion, invoice calculation, stock display helpers |
| `src/HardwarePaintShop.Infrastructure/Data/AppDbContext.cs` | EF Core DbContext with DbSets |
| `src/HardwarePaintShop.Infrastructure/Data/AppDbContextFactory.cs` | EF CLI design-time factory |
| `src/HardwarePaintShop.Desktop/App.xaml.cs` | Desktop startup, configuration, logging, DI |
| `src/HardwarePaintShop.Desktop/ViewModels` | WPF ViewModels |
| `src/HardwarePaintShop.Desktop/Views` | WPF XAML views |
| `database/phase_01_database_schema.sql` | Standalone PostgreSQL schema and seed SQL |
| `docs/*.md` | Planning and project structure documents |

No README file was found by `Get-ChildItem -Recurse -Filter README*`.

Git status: this directory is not a Git repository. `git status --short`, `git log --oneline -10`, `git branch --show-current`, and `git diff --stat` all failed with "not a git repository".

## 3. Build Status

Environment command run:

```text
dotnet --info
```

Observed SDK/runtime:

- .NET SDK: `10.0.204`
- Host: `10.0.8`
- Installed runtimes include `Microsoft.NETCore.App 8.0.28`, `Microsoft.WindowsDesktop.App 8.0.28`, `Microsoft.AspNetCore.App 10.0.8`
- No `global.json` found

Restore command run:

```text
dotnet restore HardwarePaintShopSystem.sln
```

Restore result: failed/timed out.

Observed restore errors before timeout:

```text
error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json.
error NU1301: An attempt was made to access a socket in a way forbidden by its access permissions. (api.nuget.org:443)
```

Approved network restore attempt:

```text
dotnet restore HardwarePaintShopSystem.sln
```

Result: timed out after 124 seconds with no completion output.

Build command run:

```text
dotnet build HardwarePaintShopSystem.sln --no-restore
```

Build result: failed.

Errors: 7. Warnings: 0.

Full build error pattern:

```text
C:\Program Files\dotnet\sdk\10.0.204\Sdks\Microsoft.NET.Sdk\targets\Microsoft.PackageDependencyResolution.targets(266,5): error NETSDK1004: Assets file '<project>\obj\project.assets.json' not found. Run a NuGet package restore to generate this file.
```

Projects with missing assets files:

- `src/HardwarePaintShop.Desktop/HardwarePaintShop.Desktop.csproj`
- `src/HardwarePaintShop.Infrastructure/HardwarePaintShop.Infrastructure.csproj`
- `src/HardwarePaintShop.Domain/HardwarePaintShop.Domain.csproj`
- `src/HardwarePaintShop.Application/HardwarePaintShop.Application.csproj`
- `tests/HardwarePaintShop.UnitTests/HardwarePaintShop.UnitTests.csproj`
- `tests/HardwarePaintShop.IntegrationTests/HardwarePaintShop.IntegrationTests.csproj`
- `src/HardwarePaintShop.Api/HardwarePaintShop.Api.csproj`

Likely root cause of observed build failure: restore did not complete, so NuGet assets were not generated.

Static build-risk findings after restore is fixed:

- `src/HardwarePaintShop.Application/Services/AuthService.cs`, `LookupService.cs`, `PermissionService.cs`, `UserService.cs`, `AuditService.cs`, `CashMovementService.cs`, and `StockMovementService.cs` use `HardwarePaintShop.Infrastructure.Data`, but `HardwarePaintShop.Application.csproj` references only Domain. This violates the project reference direction and is likely a compile failure.
- `src/HardwarePaintShop.Application/Services/LookupService.cs:30,40` uses `Category.ParentId`, but `src/HardwarePaintShop.Domain/Entities/Lookups.cs:12` defines `ParentCategoryId`.
- `src/HardwarePaintShop.Application/Services/LookupService.cs:64,74` and `src/HardwarePaintShop.Desktop/ViewModels/Lookups/UnitsViewModel.cs:64-65` use `Unit.Symbol` and `Unit.Notes`, but `src/HardwarePaintShop.Domain/Entities/Lookups.cs:31` defines `ShortName` and no `Notes`.
- `src/HardwarePaintShop.Desktop/App.xaml.cs:61` registers `AddDbContext<AppDbContext>`, while multiple services require `IDbContextFactory<AppDbContext>`.

## 4. Test Status

Test projects found:

- `tests/HardwarePaintShop.UnitTests/HardwarePaintShop.UnitTests.csproj`
- `tests/HardwarePaintShop.IntegrationTests/HardwarePaintShop.IntegrationTests.csproj`

Test source files found: none. The `tests` folders contain only `.csproj` files.

Command run:

```text
dotnet test HardwarePaintShopSystem.sln --no-build
```

Result: command returned exit code 0 with no output. Because restore/build did not complete and no test source files exist, no discovered/passed/failed/skipped test counts were available.

Tests discovered: not verified / no test files found.

## 5. Database Status

DbContext: `src/HardwarePaintShop.Infrastructure/Data/AppDbContext.cs`

Entity registrations: `AppDbContext` declares DbSets for lookups, users/roles, customers/suppliers, products, invoices, transactions, stock/cash, returns, inventory counts, alerts, backups, settings, audit logs, and sync.

Entity configurations: none found. `AppDbContext.cs:77` calls `ApplyConfigurationsFromAssembly`, but there is no `Configurations` folder or `IEntityTypeConfiguration<T>` file under Infrastructure.

Migrations: none found. `Get-ChildItem -Recurse -Directory -Filter Migrations` returned no entries.

Connection string: `src/HardwarePaintShop.Desktop/appsettings.json:3`

```text
Host=localhost;Port=5432;Database=hardware_paint_shop;Username=postgres;Password=***
```

Database creation readiness:

- EF Core database creation from migrations is not ready because no migrations exist.
- SQL schema creation may be possible manually from `database/phase_01_database_schema.sql`, but that schema does not match the EF entity model in several places.
- No command was run against PostgreSQL, so actual database connectivity was not verified.

Seed data:

- SQL seeds units, price groups, expense categories, roles, and one cashbox in `database/phase_01_database_schema.sql:491-533`.
- SQL does not seed users, permissions, or role_permissions.
- Default admin is not created by the SQL file or by inspected C# code.
- Password hashing exists in `PasswordHasher`, `AuthService`, and `UserService`, using BCrypt, but no seeded admin BCrypt hash exists.

Conflicting SQL schema and EF model examples:

- SQL `categories.parent_id`; EF entity uses `ParentCategoryId`.
- SQL `units.symbol`; EF entity uses `ShortName`.
- SQL `customers.opening_balance`; EF entity uses `CurrentBalance`.
- SQL `products.code` and `default_supplier_id`; EF entity uses `ProductCode` and `MainSupplierId`.
- SQL `product_costs.product_unit_id`; EF `ProductCost` has no `ProductUnitId`.
- SQL has no `owner_withdrawals`, `inventory_counts`, `inventory_count_items`, `app_settings`, `invoice_payments`, or `audit_logs` tables, while EF has DbSets/entities for them.
- SQL `product_serials.purchase_invoice_id` and `sales_invoice_id` have no FK constraints.
- SQL `returns.original_invoice_id` is a loose UUID; EF splits original sales and purchase invoice IDs.

Unique indexes:

- SQL declares several unique constraints and indexes.
- EF Fluent unique indexes are not configured because no configurations exist.
- EF conventions may create primary keys and relationships, but unique constraints such as barcode uniqueness and product/unit composite uniqueness are not explicitly configured in inspected EF code.

## 6. Feature Completion Matrix

| Module | Status | Evidence Files | Missing Work | Severity |
|---|---|---|---|---|
| 1. Solution and project structure | Partial | `HardwarePaintShopSystem.sln`, all `.csproj` files | Restore/build not passing; `.slnx` is a 25-byte placeholder-like file | High |
| 2. Dependency injection | Broken | `src/HardwarePaintShop.Desktop/App.xaml.cs:61-90` | Register `IDbContextFactory<AppDbContext>` or change services; register password/repository/stock/cash/audit services as needed | Critical |
| 3. Configuration and appsettings | Partial | `src/HardwarePaintShop.Desktop/appsettings.json` | Secret placeholder remains; no environment override inspected; DB name conflicts with docs SQL example | High |
| 4. PostgreSQL connection | Partial | `App.xaml.cs:60-62`, `AppDbContextFactory.cs:26-29` | Not runtime verified; connection password placeholder; restore/build blocked | High |
| 5. EF Core DbContext | Partial | `AppDbContext.cs` | No configurations; model/schema mismatches; no migrations | Critical |
| 6. EF Core migrations | Missing | No `Migrations` directory found | Create initial migration after model is corrected | Critical |
| 7. Domain entities | Partial | `src/HardwarePaintShop.Domain/Entities/*.cs` | Broad model exists but has mismatches with services and SQL schema | High |
| 8. Enums | Partial | `src/HardwarePaintShop.Domain/Enums/*.cs` | Present, but no EF enum conversion configured | Medium |
| 9. Repositories | Partial | `Application/Interfaces/IRepository.cs`, `Infrastructure/Repositories/*.cs` | Not registered in DI; duplicate Infrastructure `IRepository` differs from Application interface | Medium |
| 10. Application services | Broken | `Application/Services/*.cs` | Application depends on Infrastructure; several entity property mismatches; business workflows mostly absent | Critical |
| 11. Login | Partial | `LoginWindow.xaml`, `LoginViewModel.cs`, `AuthService.cs` | Cannot work until DI/build/DB/admin seed fixed | Critical |
| 12. Password hashing | Partial | `PasswordHasher.cs`, `UserService.cs`, `AuthService.cs` | Hasher not registered; no default admin hash seeded; direct BCrypt use duplicated | Medium |
| 13. Default admin seed | Missing | SQL seed section, C# services | No default user insert or initializer found | Critical |
| 14. Roles and permissions | Partial | `Users.cs`, `PermissionService.cs`, `RolesViewModel.cs`, SQL roles seed | No permissions seed; no role_permissions seed; permissions not loaded after login | High |
| 15. Main Window and navigation | Partial | `MainWindow.xaml`, `MainViewModel.cs`, `NavigationService.cs` | Many pages return `null`; `NavigationService` TODO unused | High |
| 16. Dashboard | Placeholder | `DashboardView.xaml`, `DashboardViewModel.cs:32` | Shows zero values; refresh is `Task.CompletedTask` | Medium |
| 17. Categories | Broken | `CategoriesViewModel.cs`, `CategoriesView.xaml`, `LookupService.cs` | Uses missing `Category.ParentId`; build risk | Critical |
| 18. Units | Broken | `UnitsViewModel.cs`, `UnitsView.xaml`, `LookupService.cs` | Uses missing `Unit.Symbol` and `Unit.Notes`; XAML binds `Units` but VM exposes `Items` | Critical |
| 19. Price Groups | Broken | `PriceGroupsViewModel.cs`, `PriceGroupsView.xaml` | XAML binds `PriceGroups`, `NewPriceGroupCommand`, `EditPriceGroupCommand`; VM exposes `Items`, `NewItemCommand`, `EditItemCommand` | High |
| 20. Expense Categories | Broken | `ExpenseCategoriesViewModel.cs`, `ExpenseCategoriesView.xaml` | XAML binds `ExpenseCategories`, `NewExpenseCategoryCommand`, `EditExpenseCategoryCommand`; VM exposes `Items`, `NewItemCommand`, `EditItemCommand` | High |
| 21. Users | Partial | `UsersViewModel.cs`, `UsersView.xaml`, `UserService.cs` | Depends on broken Application/Infrastructure direction and DB factory registration | High |
| 22. Roles and permission assignment | Partial | `RolesViewModel.cs`, `RolesView.xaml`, `UserService.cs` | No permission seed; role permission model/SQL PK mismatch; no permission enforcement in UI | High |
| 23. Products | Missing | Domain entity only: `Products.cs` | No product service, ViewModels, Views, API, or CRUD workflow | High |
| 24. Product units | Missing | Domain entity only: `Products.cs` | No product-unit workflow | High |
| 25. Product prices | Missing | Domain entity only: `Products.cs` | No pricing workflow | High |
| 26. Product barcodes | Missing | Domain entity only: `Products.cs` | No barcode workflow or scanner UI | Medium |
| 27. Product serial numbers | Missing | Domain entity only: `Products.cs` | No serial workflow | Medium |
| 28. Product images | Missing | `Product.ImagePath` only | No image storage/service/UI; documented folders absent | Medium |
| 29. Price inquiry | Missing | Navigation key only in `MainViewModel.cs` | No view/service | High |
| 30. Customers | Missing | Domain entity only: `Parties.cs` | No customer service/ViewModel/View | High |
| 31. Suppliers | Missing | Domain entity only: `Parties.cs` | No supplier service/ViewModel/View | High |
| 32. Sales invoices | Missing | Domain entity only: `Invoices.cs` | No sales invoice service/UI/transactions | Critical |
| 33. Purchase invoices | Missing | Domain entity only: `Invoices.cs` | No purchase invoice service/UI/transactions | Critical |
| 34. Stock movements | Partial | `IStockMovementService.cs`, `Application/Services/StockMovementService.cs`, `Infrastructure/Services/StockMovementService.cs` | Duplicate implementations; not registered; no transaction integration | High |
| 35. Cashbox | Partial | `ICashMovementService.cs`, `CashMovementService.cs`, `Finance.cs` | No UI; services not registered; no transaction boundaries | High |
| 36. Expenses | Missing | Domain entity only plus expense category lookup | No expense service/UI | High |
| 37. Owner withdrawals and deposits | Partial | `OwnerWithdrawal` entity, `CashMovementType.OwnerWithdrawal/OwnerDeposit` | No owner deposit entity; no UI/service workflow | Medium |
| 38. Customer collections | Missing | `InvoicePayment`, `CustomerTransaction` entities only | No collection workflow | High |
| 39. Supplier payments | Missing | `InvoicePayment`, `SupplierTransaction` entities only | No payment workflow | High |
| 40. Sales returns | Missing | `Return`, `ReturnItem` entities only | No return service/UI | High |
| 41. Purchase returns | Missing | `Return`, `ReturnItem` entities only | No return service/UI | High |
| 42. Inventory count | Missing | Entities only | No inventory count service/UI; SQL lacks tables | High |
| 43. Alerts | Missing | `Alert` entity, nav key only | No alert service/UI; SQL alert model differs from EF | Medium |
| 44. Reports | Missing | Navigation key and docs only | No report services/views | Medium |
| 45. Thermal printing | Missing | Docs only | No printing folder/service/package | Medium |
| 46. A4 printing | Missing | Docs only | No A4 printing implementation | Medium |
| 47. Backup and restore | Missing | `BackupLog` entity, appsettings backup values | No backup service/UI; SQL model differs from EF | Medium |
| 48. Audit logging | Partial | `IAuditService.cs`, `AuditService.cs`, `AuditLog` entity | Not registered; no automatic audit integration; SQL lacks audit_logs table | High |
| 49. ASP.NET Core API | Placeholder | `src/HardwarePaintShop.Api/HardwarePaintShop.Api.csproj` only | No `Program.cs`, controllers, middleware, endpoints | High |
| 50. Mobile synchronization | Missing | `SyncDevice`, `SyncLog` entities only | No API/sync implementation | Medium |
| 51. Unit tests | Missing | `tests/HardwarePaintShop.UnitTests/*.csproj` | No test source files | High |
| 52. Integration tests | Missing | `tests/HardwarePaintShop.IntegrationTests/*.csproj` | No test source files | High |

## 7. Navigation and UI Status

Startup:

- `App.xaml` sets `ShutdownMode="OnMainWindowClose"`.
- `App.xaml.cs` builds configuration from `appsettings.json`, configures Serilog file logging, configures DI, and opens `LoginWindow`.

Login to Main Window:

- `LoginWindow.xaml.cs` subscribes to `LoginSucceeded`, resolves `MainWindow`, shows it, then closes login.
- This is structurally implemented, but actual login depends on build, DI, database connectivity, and a seeded user.

Navigation:

- `MainViewModel` builds many navigation items.
- `ResolveView` maps only Dashboard, Categories, Units, PriceGroups, ExpenseCategories, Users, and Roles.
- All other sidebar items return `null` at `MainViewModel.cs:85`.
- `NavigationService.cs:22` contains a TODO and is not used by `MainViewModel` navigation.

DataTemplates/ViewModel mapping:

- Views are created manually in `MainViewModel.Resolve<TView,TViewModel>()` and DataContext is assigned from `App.Services`.
- There are no WPF DataTemplates mapping ViewModels to Views in `App.xaml`.

ViewModel registrations:

- Registered: Login, Main, Dashboard, Categories, Units, PriceGroups, ExpenseCategories, Users, Roles.
- Not registered: product/customer/supplier/sales/purchase/returns/inventory/cashbox/reports/alerts/backup/settings ViewModels because those ViewModels do not exist.

Visible UI/runtime problems:

- Lookup XAML binding mismatches:
  - `ExpenseCategoriesView.xaml:62,77,87` binds non-existent command/properties compared to `ExpenseCategoriesViewModel`.
  - `PriceGroupsView.xaml:68,83,94` binds non-existent command/properties compared to `PriceGroupsViewModel`.
  - `UnitsView.xaml:67,84,95` binds non-existent command/properties compared to `UnitsViewModel`.
- `DashboardView.xaml:31` binds `RefreshAsyncCommand`; CommunityToolkit normally generates `RefreshCommand` from `RefreshAsync`, not `RefreshAsyncCommand`.
- `App.xaml` does not merge `Styles/Cards.xaml`, but `CardStyle` is also defined in `Forms.xaml`, so this is not necessarily a missing resource.

## 8. Security and Configuration Findings

- `src/HardwarePaintShop.Desktop/appsettings.json:3` contains a hardcoded PostgreSQL username and a placeholder password. Secret redacted in this report.
- No user/admin seed exists, so login cannot succeed on a fresh database.
- BCrypt hashing exists, but `IPasswordHasher` is not registered and services directly call BCrypt.
- Roles are seeded in SQL, but permissions and role assignments are not seeded.
- `PermissionService.LoadPermissionsAsync()` exists, but no inspected login flow calls it.
- `ForcePasswordChange` exists on `User`, but no login enforcement was found.

## 9. Placeholder and Incomplete Code Findings

| File | Line | Finding | Why it matters |
|---|---:|---|---|
| `src/HardwarePaintShop.Desktop/appsettings.json` | 3 | `Password=CHANGE_ME` | Application cannot connect to PostgreSQL until configured; secret should not be committed as a real password |
| `src/HardwarePaintShop.Infrastructure/Repositories/Repository.cs` | 42 | `return Task.CompletedTask;` in `UpdateAsync` | Acceptable for EF state update, but repository is not registered or used |
| `src/HardwarePaintShop.Desktop/NavigationService.cs` | 22 | TODO: resolve and display pages by key | Registered navigation service is a placeholder |
| `src/HardwarePaintShop.Desktop/ViewModels/Dashboard/DashboardViewModel.cs` | 32 | `Task.CompletedTask; // wired in Milestone 11` | Dashboard refresh is a placeholder |
| `src/HardwarePaintShop.Desktop/ViewModels/Shell/MainViewModel.cs` | 85 | `_ => null // stub for not-yet-built screens` | Most navigation buttons render blank content |

Search terms run:

```text
TODO, FIXME, NotImplementedException, throw new Exception, placeholder, mock, dummy, temporary, CurrentPage = null, => null, return null, return new List, return Array.Empty, Task.CompletedTask, Milestone, stub, CHANGE_ME, Password=, Username=
```

No `FIXME`, `NotImplementedException`, `throw new Exception`, `mock`, `dummy`, `temporary`, or `CurrentPage = null` occurrences were found by the search command.

## 10. Critical Problems

1. Restore/build is not currently successful. The observed build failure is missing NuGet assets after restore failure/timeouts.
2. Application layer references Infrastructure types without a project reference, likely creating compile errors after restore.
3. DI registers `AppDbContext`, but services require `IDbContextFactory<AppDbContext>`.
4. Lookup services/ViewModels reference entity properties that do not exist.
5. No EF migrations exist.
6. EF model and SQL schema conflict.
7. No default admin or permission seed exists.
8. API project has only a `.csproj`; no `Program.cs` or endpoints.

## 11. Non-Critical Problems

1. Many planned folders in `docs/project_structure.md` do not exist yet.
2. `HardwarePaintShop.slnx` exists but is only 25 bytes.
3. Several source files display mojibake/encoding issues in terminal output for Arabic text.
4. `NavigationService` is registered but unused.
5. Duplicate stock/cash/audit service implementations exist in Application and Infrastructure.
6. Tests projects exist without test files.

## 12. Files That Need Review

- `src/HardwarePaintShop.Application/HardwarePaintShop.Application.csproj`
- `src/HardwarePaintShop.Application/Services/*.cs`
- `src/HardwarePaintShop.Domain/Entities/Lookups.cs`
- `src/HardwarePaintShop.Infrastructure/Data/AppDbContext.cs`
- `src/HardwarePaintShop.Desktop/App.xaml.cs`
- `src/HardwarePaintShop.Desktop/ViewModels/Lookups/*.cs`
- `src/HardwarePaintShop.Desktop/Views/Lookups/*.xaml`
- `src/HardwarePaintShop.Desktop/ViewModels/Dashboard/DashboardViewModel.cs`
- `database/phase_01_database_schema.sql`
- `src/HardwarePaintShop.Desktop/appsettings.json`

## 13. Exact Current Project Stage

The project is at scaffold/foundation stage, before a verified runnable MVP. Domain modeling is broad, initial WPF shell/login/lookup/user screens exist, and a standalone SQL schema exists, but the codebase is not restore/build verified and core business modules are missing.

## 14. Recommended Next Milestone

Recommended next milestone: make the foundation buildable and align the Application/Infrastructure/Domain boundary before adding features.

The next milestone should fix project reference direction, DI registration, entity/property mismatches, and EF schema strategy. Product/sales/customer feature work should wait until the solution builds and the database model is coherent.
