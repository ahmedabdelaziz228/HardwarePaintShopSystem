# Foundation Stabilization Report (Historical)

> This document records an earlier milestone. The placeholder routes mentioned here were replaced by implemented RC1 modules; see `VERSION_1_0_RC1_ACCEPTANCE.md`.

## Summary

Task: Stabilize Foundation Build and Data Access Wiring.

Result: the solution now restores, builds with 0 errors and 0 warnings, and runs 10 focused unit tests successfully. The EF initial migration was not created because `dotnet-ef` is not installed or available on PATH.

## Files Changed

Added:

- `README.md`
- `src/HardwarePaintShop.Api/Program.cs`
- `src/HardwarePaintShop.Application/Interfaces/IDatabaseInitializer.cs`
- `src/HardwarePaintShop.Application/Security/PermissionCodes.cs`
- `src/HardwarePaintShop.Desktop/DesktopServiceRegistration.cs`
- `src/HardwarePaintShop.Desktop/Views/Shell/NotImplementedView.xaml`
- `src/HardwarePaintShop.Desktop/Views/Shell/NotImplementedView.xaml.cs`
- `src/HardwarePaintShop.Infrastructure/Data/Configurations/CoreEntityConfiguration.cs`
- `src/HardwarePaintShop.Infrastructure/Data/DatabaseInitializer.cs`
- `src/HardwarePaintShop.Infrastructure/Services/AuthService.cs`
- `src/HardwarePaintShop.Infrastructure/Services/LookupService.cs`
- `src/HardwarePaintShop.Infrastructure/Services/PermissionService.cs`
- `src/HardwarePaintShop.Infrastructure/Services/UserService.cs`
- `tests/HardwarePaintShop.UnitTests/FoundationTests.cs`
- `docs/FOUNDATION_STABILIZATION_REPORT.md`

Updated:

- `src/HardwarePaintShop.Application/Interfaces/IAuthService.cs`
- `src/HardwarePaintShop.Application/Services/PasswordHasher.cs`
- `src/HardwarePaintShop.Desktop/App.xaml.cs`
- `src/HardwarePaintShop.Desktop/HardwarePaintShop.Desktop.csproj`
- `src/HardwarePaintShop.Desktop/ViewModels/LoginViewModel.cs`
- `src/HardwarePaintShop.Desktop/ViewModels/MainViewModel.cs`
- `src/HardwarePaintShop.Desktop/ViewModels/Lookups/CategoriesViewModel.cs`
- `src/HardwarePaintShop.Desktop/ViewModels/Lookups/UnitsViewModel.cs`
- `src/HardwarePaintShop.Desktop/ViewModels/Lookups/PriceGroupsViewModel.cs`
- `src/HardwarePaintShop.Desktop/ViewModels/Lookups/ExpenseCategoriesViewModel.cs`
- `src/HardwarePaintShop.Desktop/Views/MainWindow.xaml.cs`
- `src/HardwarePaintShop.Desktop/Views/Dashboard/DashboardView.xaml`
- `src/HardwarePaintShop.Desktop/Views/Lookups/CategoriesView.xaml`
- `src/HardwarePaintShop.Desktop/Views/Lookups/UnitsView.xaml`
- `src/HardwarePaintShop.Desktop/Views/Lookups/PriceGroupsView.xaml`
- `src/HardwarePaintShop.Desktop/Views/Lookups/ExpenseCategoriesView.xaml`
- `src/HardwarePaintShop.Desktop/Views/Users/UsersView.xaml`
- `src/HardwarePaintShop.Desktop/Views/Roles/RolesView.xaml`
- `src/HardwarePaintShop.Infrastructure/Data/AppDbContext.cs`
- `src/HardwarePaintShop.Infrastructure/Services/AuditService.cs`
- `src/HardwarePaintShop.Infrastructure/Services/CashMovementService.cs`
- `src/HardwarePaintShop.Infrastructure/Services/StockMovementService.cs`
- `tests/HardwarePaintShop.UnitTests/HardwarePaintShop.UnitTests.csproj`

Removed or consolidated:

- Application EF-backed service implementations were removed so Application no longer references Infrastructure.
- The duplicate Infrastructure repository interface was removed; `Repository<T>` now uses the Application repository interface.
- The unused Desktop navigation service placeholder was removed.

## Architecture Decision Made

Application now owns service contracts and business-facing helper code. Infrastructure owns EF Core data access implementations. Desktop is the composition root.

Verified static check:

```powershell
rg -n "HardwarePaintShop\.Infrastructure" src\HardwarePaintShop.Application
```

Result: no matches.

## DI Changes

`DesktopServiceRegistration` now centralizes WPF dependency injection. It registers `AddDbContextFactory<AppDbContext>` instead of scoped `AddDbContext`, which avoids desktop scoped-lifetime problems.

The following important services and view models are registered and covered by a DI unit test:

- `LoginViewModel`
- `MainViewModel`
- `DashboardViewModel`
- `CategoriesViewModel`
- `UnitsViewModel`
- `PriceGroupsViewModel`
- `ExpenseCategoriesViewModel`
- `UsersViewModel`
- `RolesViewModel`
- `IAuthService`
- `IPermissionService`
- `ILookupService`
- `IUserService`
- `IPasswordHasher`
- `IAuditService`
- `IStockMovementService`
- `ICashMovementService`

Window registrations for `LoginWindow` and `MainWindow` are also validated as service descriptors.

## Lookup Fixes

Lookup code now follows Domain property names:

- Category parent references use `ParentCategoryId`.
- Category navigation uses the existing `ParentCategory` relationship.
- Unit display and editing use `ShortName`.
- Unit notes usage was removed from the view model path because the Domain `Unit` entity does not define `Notes`.

The Categories, Units, Price Groups, and Expense Categories views were aligned with their view model properties and generated CommunityToolkit command names.

## Navigation Fixes

The selected navigation strategy is resolved WPF views assigned to `MainViewModel.CurrentPage`.

Implemented pages:

- Dashboard
- Categories
- Units
- Price Groups
- Expense Categories
- Users
- Roles

Unimplemented sidebar targets now display `NotImplementedView` with the Arabic text `هذه الشاشة لم تُنفذ بعد`, instead of returning a blank or null page.

## EF Model Changes

Added explicit Fluent API configuration under:

`src/HardwarePaintShop.Infrastructure/Data/Configurations`

Configured areas include:

- Category self-reference.
- User to Role.
- Unique RolePermission pair on `RoleId` and `PermissionId`.
- Product relationships.
- Unique ProductUnit pair on `ProductId` and `UnitId`.
- Unique ProductPrice pair on `ProductUnitId` and `PriceGroupId`.
- Unique ProductBarcode barcode.
- Unique ProductSerial serial number.
- Customer and Supplier indexes.
- Unique SalesInvoice invoice number.
- Unique PurchaseInvoice invoice number.
- StockMovement indexes.
- CashMovement indexes.
- Unique InvoicePayment payment number.
- Unique Return return number.
- Unique InventoryCount count number.
- Unique Permission code.
- Unique User username.

Decimal precision convention:

- Money: `18,2`
- Quantity: `18,3`
- Conversion factor: `18,4`

Enums are configured as strings. Delete behavior is restrictive for historical records. Cascade delete is limited to safe child collections such as invoice items, purchase invoice items, return items, and inventory count items.

## Seed Strategy

`DatabaseInitializer` performs first-run initialization. It applies migrations when migrations exist, otherwise it falls back to `EnsureCreatedAsync` so a fresh development database can be initialized before migrations are available.

Seeded roles:

- Owner
- Admin
- Cashier
- StoreKeeper

Seeded permissions:

- `Dashboard.View`
- `Category.View`
- `Category.Create`
- `Category.Update`
- `Category.Deactivate`
- `Unit.View`
- `Unit.Create`
- `Unit.Update`
- `Unit.Deactivate`
- `PriceGroup.View`
- `PriceGroup.Create`
- `PriceGroup.Update`
- `PriceGroup.Deactivate`
- `ExpenseCategory.View`
- `ExpenseCategory.Create`
- `ExpenseCategory.Update`
- `ExpenseCategory.Deactivate`
- `User.View`
- `User.Create`
- `User.Update`
- `User.Deactivate`
- `Role.View`
- `Role.UpdatePermissions`

Owner receives all permissions.

Default development admin:

- Username: `admin`
- Password: `admin123`
- `ForcePasswordChange`: `true`

The password is stored through BCrypt hashing. No plain-text admin password is inserted into the database.

Login now loads permissions after successful authentication. `ForcePasswordChange` is detected and reported in the UI; the password-change screen remains deferred.

## Migration Result

Migration not created.

Command:

```powershell
dotnet ef --version
```

Result:

```text
dotnet-ef does not exist.
```

Command attempted:

```powershell
dotnet ef migrations add InitialCreate --project src\HardwarePaintShop.Infrastructure --startup-project src\HardwarePaintShop.Desktop
```

Result: failed for the same missing `dotnet-ef` tool reason.

Precise blocker: the .NET EF Core command-line tool is not installed or not available on PATH. A live PostgreSQL server is not required for migration generation.

Next command after installing the tool:

```powershell
dotnet ef migrations add InitialCreate --project src\HardwarePaintShop.Infrastructure --startup-project src\HardwarePaintShop.Desktop
```

## Restore Result

Successful with network access:

```powershell
dotnet restore HardwarePaintShopSystem.sln --disable-parallel
```

Result: success.

Earlier sandboxed restore attempts failed only because NuGet network access was blocked. The observed errors included socket access denial and host resolution failure for `api.nuget.org`.

## Build Result

Command:

```powershell
dotnet build HardwarePaintShopSystem.sln --no-restore
```

Result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Test Result

Command:

```powershell
dotnet test HardwarePaintShopSystem.sln --no-build
```

Result:

```text
Passed: 10
Failed: 0
Skipped: 0
Total: 10
```

The integration test assembly currently has no discoverable tests.

Focused tests added:

- `UnitConverter_ToBase_Works`
- `UnitConverter_FromBase_Works`
- `InvoiceCalculator_LineTotal_Works`
- `InvoiceCalculator_ReturnsPaidStatus`
- `InvoiceCalculator_ReturnsPartialStatus`
- `InvoiceCalculator_ReturnsUnpaidStatus`
- `PasswordHasher_HashAndVerify_Works`
- `DI_ImportantServicesCanResolve`
- `LookupService_UsesCorrectCategoryParentProperty`
- `Lookup_ViewModelsExposePropertiesAndCommandsUsedByXaml`

## Remaining Blockers

- `dotnet-ef` is unavailable, so the initial migration is still missing.
- PostgreSQL connectivity was not verified against a live server.
- Integration tests remain empty.
- The password-change screen is deferred; only `ForcePasswordChange` detection and reporting are implemented.
- API remains a minimal host with no business endpoints, by task constraint.

## Recommended Next Task

Exact next task: Create Initial EF Core Migration and Verify Fresh Database Startup.

Why this should be next: the model now builds and has explicit configuration, but the database source of truth cannot be complete until `InitialCreate` exists and a fresh database can be created from it.

Dependencies already completed:

- Clean Architecture dependency direction restored.
- DI service registration stabilized.
- EF configurations added.
- First-run seed initializer added.
- Build and unit tests pass.

Dependencies still missing:

- Install or expose `dotnet-ef`.
- Valid local PostgreSQL connection string for a development database.

Acceptance criteria for the next task:

- `dotnet ef migrations add InitialCreate --project src\HardwarePaintShop.Infrastructure --startup-project src\HardwarePaintShop.Desktop` succeeds.
- `dotnet ef database update --project src\HardwarePaintShop.Infrastructure --startup-project src\HardwarePaintShop.Desktop` succeeds against a development database.
- Fresh startup creates roles, permissions, and BCrypt-hashed admin.
- Login with `admin` / `admin123` succeeds and loads permissions.
- Existing build and unit tests continue to pass.

Things the next agent must not change:

- Do not add Products, Customers, Sales, Purchases, Reports, API endpoints, or mobile sync.
- Do not delete the SQL schema reference.
- Do not commit real database passwords.
- Do not break the rule that stock changes use `StockMovement` and cash changes use `CashMovement`.
