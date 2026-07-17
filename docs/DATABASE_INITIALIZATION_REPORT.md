# Database Initialization Report

## 1. Executive Summary

The repository now has a local `dotnet-ef` tool manifest pinned to EF Core 8.0.11 and an `InitialCreate` EF Core migration. Design-time DbContext creation succeeds with the Npgsql provider.

The migration could not be applied to PostgreSQL because no server was accepting connections on `127.0.0.1:5432`. Seed data, admin authentication against PostgreSQL, and desktop startup against PostgreSQL were therefore not live-verified.

Final regression status: restore succeeded, build succeeded with 0 errors and 0 warnings, unit tests passed, and one live PostgreSQL integration test is present but skipped until a dedicated test database connection string is configured.

## 2. dotnet-ef Installation Result

Repository-local tool manifest:

`D:\New folder (2)\HardwarePaintShopSystem\.config\dotnet-tools.json`

Installed tool:

- `dotnet-ef`
- Version: `8.0.11`

Commands run:

```powershell
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 8.0.11
dotnet tool restore
```

The .NET 10 SDK listed the local tool correctly, but `dotnet ef --version` and `dotnet tool run dotnet-ef --version` reported:

```text
Run "dotnet tool restore" to make the "dotnet-ef" command available.
```

After restoring, the tool DLL itself executed successfully:

```powershell
dotnet "C:\Users\lap shop\.nuget\packages\dotnet-ef\8.0.11\tools\net8.0\any\dotnet-ef.dll" --version
```

Result:

```text
Entity Framework Core .NET Command-line Tools
8.0.11
```

## 3. DbContext Design-Time Validation

Command:

```powershell
dotnet "C:\Users\lap shop\.nuget\packages\dotnet-ef\8.0.11\tools\net8.0\any\dotnet-ef.dll" dbcontext info --project src\HardwarePaintShop.Infrastructure --startup-project src\HardwarePaintShop.Desktop
```

Result:

```text
Build succeeded.
Type: HardwarePaintShop.Infrastructure.Data.AppDbContext
Provider name: Npgsql.EntityFrameworkCore.PostgreSQL
Database name: hardware_paint_shop
Data source: tcp://localhost:5432
Options: None
```

Design-time support changes:

- `src/HardwarePaintShop.Desktop/HardwarePaintShop.Desktop.csproj` now references `Microsoft.EntityFrameworkCore.Design` 8.0.11 with `PrivateAssets=all`.
- `src/HardwarePaintShop.Infrastructure/Data/AppDbContextFactory.cs` supports `HARDWARE_PAINT_SHOP_CONNECTION_STRING` before falling back to `appsettings.json`.

## 4. Initial Migration Result

Command:

```powershell
dotnet "C:\Users\lap shop\.nuget\packages\dotnet-ef\8.0.11\tools\net8.0\any\dotnet-ef.dll" migrations add InitialCreate --project src\HardwarePaintShop.Infrastructure --startup-project src\HardwarePaintShop.Desktop --output-dir Data\Migrations
```

Result:

```text
Build succeeded.
Done.
```

`InitialCreate` was created successfully.

## 5. Generated Migration Files

Generated files:

- `src/HardwarePaintShop.Infrastructure/Data/Migrations/20260612150442_InitialCreate.cs`
- `src/HardwarePaintShop.Infrastructure/Data/Migrations/20260612150442_InitialCreate.Designer.cs`
- `src/HardwarePaintShop.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs`

Inspection findings:

- No EF `HasData` seed data was generated.
- Enum properties are represented as text columns in the migration/snapshot.
- Decimal precision appears in generated columns as `numeric(18,2)`, `numeric(18,3)`, and `numeric(18,4)`.
- Expected unique indexes are present in the migration for usernames, permission codes, product codes, barcodes, serial numbers, invoice numbers, payment numbers, return numbers, inventory count numbers, and role-permission pairs.
- Historical relationships are generated with `ReferentialAction.Restrict`.
- Cascade delete appears only on intended child collections such as invoice items, return items, and inventory count items.

## 6. PostgreSQL Availability

Commands:

```powershell
where.exe psql
where.exe pg_isready
```

Results:

```text
INFO: Could not find files for the given pattern(s).
```

Configured connection string metadata, with password masked:

```text
Host=localhost;Port=5432;Database=hardware_paint_shop;Username=postgres;Password=***
EnvConnectionStringSupplied=False
TestConnectionStringSupplied=False
```

The checked-in password is the placeholder `CHANGE_ME`; it was not printed.

## 7. Database Update Result

Command:

```powershell
dotnet "C:\Users\lap shop\.nuget\packages\dotnet-ef\8.0.11\tools\net8.0\any\dotnet-ef.dll" database update --project src\HardwarePaintShop.Infrastructure --startup-project src\HardwarePaintShop.Desktop
```

Result:

```text
Build succeeded.
Failed to connect to 127.0.0.1:5432
No connection could be made because the target machine actively refused it.
```

Database update was not applied because PostgreSQL is not running or not reachable on localhost port 5432.

## 8. Tables and Indexes Verification

Database-level verification was not run because the migration was not applied.

Migration-level verification found the expected unique indexes:

- `IX_Users_Username`
- `IX_Permissions_Code`
- `IX_Products_ProductCode`
- `IX_ProductBarcodes_Barcode`
- `IX_ProductSerials_SerialNumber`
- `IX_SalesInvoices_InvoiceNo`
- `IX_PurchaseInvoices_InvoiceNo`
- `IX_InvoicePayments_PaymentNo`
- `IX_Returns_ReturnNo`
- `IX_InventoryCounts_CountNo`
- `IX_RolePermissions_RoleId_PermissionId`

## 9. Seed Data Verification

Seed data was not verified against PostgreSQL because the database update could not connect to PostgreSQL.

Code-level seed strategy is present in:

`src/HardwarePaintShop.Infrastructure/Data/DatabaseInitializer.cs`

The initializer now uses:

```csharp
await db.Database.MigrateAsync(cancellationToken);
```

It no longer uses `EnsureCreatedAsync`.

Expected seed data:

- Roles: Owner, Admin, Cashier, StoreKeeper.
- Permissions from `PermissionCodes.All`.
- Owner role receives all permissions.
- Development admin user `admin`, active, assigned to Owner, with `ForcePasswordChange = true`.
- Admin password is hashed with BCrypt.

## 10. Default Admin Authentication

Default admin authentication was not verified against a live PostgreSQL database because PostgreSQL was unavailable.

A guarded service-level integration test was added:

`tests/HardwarePaintShop.IntegrationTests/DatabaseInitializationTests.cs`

The test verifies:

- Initializer idempotency.
- Role and permission seed counts.
- Owner permission assignment.
- Admin user status and role.
- Admin password hash is not plain text.
- BCrypt verification for `admin123`.
- Incorrect password fails.
- Correct password succeeds.
- Force password change is returned.

The test is skipped until a dedicated PostgreSQL test database connection string is configured.

## 11. Permission Loading Verification

Permission loading was not live-verified against PostgreSQL because PostgreSQL was unavailable.

The guarded integration test verifies permission loading for the implemented foundation modules when enabled against a dedicated test database.

## 12. Desktop Startup Verification

Desktop startup was not manually verified because PostgreSQL was unavailable and GUI interaction is not reliable in the current execution environment.

Expected blocker if run now: database initialization attempts to connect to localhost:5432 and the connection is refused.

## 13. Build Result

Final command:

```powershell
dotnet build HardwarePaintShopSystem.sln --no-restore
```

Result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## 14. Test Result

Final command:

```powershell
dotnet test HardwarePaintShopSystem.sln --no-build
```

Result:

```text
HardwarePaintShop.UnitTests: Passed 10, Failed 0, Skipped 0
HardwarePaintShop.IntegrationTests: Passed 0, Failed 0, Skipped 1
Total: Passed 10, Failed 0, Skipped 1
```

The skipped integration test requires `HARDWARE_PAINT_SHOP_TEST_CONNECTION_STRING` and a dedicated database name containing `test` or `dev_verify`.

## 15. Remaining External Blockers

- PostgreSQL CLI tools are not on PATH: `psql` and `pg_isready` were not found.
- No PostgreSQL server is accepting connections on `127.0.0.1:5432`.
- The checked-in connection string still contains the placeholder password `CHANGE_ME`.
- `dotnet ef` local-tool alias did not execute under the installed .NET 10 SDK despite a restored local tool; the restored tool DLL worked.
- Live database update, seed verification, admin login verification, and desktop startup verification remain blocked until PostgreSQL and a safe development connection string are available.

Commands to run after PostgreSQL is available:

```powershell
dotnet tool restore
$env:HARDWARE_PAINT_SHOP_CONNECTION_STRING="Host=localhost;Port=5432;Database=hardware_paint_shop;Username=postgres;Password=***"
dotnet ef database update --project src\HardwarePaintShop.Infrastructure --startup-project src\HardwarePaintShop.Desktop
$env:HARDWARE_PAINT_SHOP_TEST_CONNECTION_STRING="Host=localhost;Port=5432;Database=hardware_paint_shop_dev_verify;Username=postgres;Password=***"
dotnet test tests\HardwarePaintShop.IntegrationTests\HardwarePaintShop.IntegrationTests.csproj
```

## 16. Recommended Next Task

Recommended next task: Verify PostgreSQL Runtime Initialization on a Dedicated Development Database.

Scope:

- Start or install PostgreSQL outside this task with a known safe development password.
- Apply `InitialCreate` to `hardware_paint_shop` or a clearly dedicated verification database.
- Run the guarded integration test against `hardware_paint_shop_dev_verify`.
- Manually launch the WPF desktop app and verify LoginWindow, admin login, permission loading, MainWindow, implemented navigation pages, and placeholder pages.

Do not start Products, Customers, Suppliers, Sales, Purchases, Returns, Reports, API endpoints, or Mobile features until live database initialization and desktop startup are verified.
