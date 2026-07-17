# Recommended Next Task

## Exact task name

Stabilize Foundation Build and Data Access Wiring

## Why this should be next

The repository cannot currently be build-verified because restore did not complete and `dotnet build --no-restore` fails with missing `project.assets.json` files. Static inspection also shows likely compile/runtime blockers once restore succeeds: Application services reference Infrastructure, DI does not register `IDbContextFactory<AppDbContext>`, and lookup services/ViewModels use entity members that do not exist.

Adding product, sales, or customer features before this would build on an unstable foundation.

## Dependencies already completed

- Solution and project scaffold exists.
- WPF Desktop project exists.
- Domain entity and enum files exist.
- `AppDbContext` declares broad DbSet coverage.
- Basic login, shell, dashboard, lookup, user, and role View/ViewModel files exist.
- PostgreSQL connection string location exists in `src/HardwarePaintShop.Desktop/appsettings.json`.
- Standalone SQL schema exists in `database/phase_01_database_schema.sql`.

## Dependencies still missing

- Successful NuGet restore.
- Successful solution build.
- Correct Application/Infrastructure dependency direction.
- Correct DI registrations for DbContext factory and services.
- Consistent lookup entity/service/ViewModel/XAML property names.
- EF migrations or a deliberate decision to use SQL schema only.
- Default admin and permission seed.
- Unit tests for basic helpers/services.

## Files that should be modified

- `src/HardwarePaintShop.Application/HardwarePaintShop.Application.csproj`
- `src/HardwarePaintShop.Application/Services/AuthService.cs`
- `src/HardwarePaintShop.Application/Services/LookupService.cs`
- `src/HardwarePaintShop.Application/Services/PermissionService.cs`
- `src/HardwarePaintShop.Application/Services/UserService.cs`
- `src/HardwarePaintShop.Application/Services/AuditService.cs`
- `src/HardwarePaintShop.Application/Services/CashMovementService.cs`
- `src/HardwarePaintShop.Application/Services/StockMovementService.cs`
- `src/HardwarePaintShop.Desktop/App.xaml.cs`
- `src/HardwarePaintShop.Domain/Entities/Lookups.cs`
- `src/HardwarePaintShop.Desktop/ViewModels/Lookups/CategoriesViewModel.cs`
- `src/HardwarePaintShop.Desktop/ViewModels/Lookups/UnitsViewModel.cs`
- `src/HardwarePaintShop.Desktop/ViewModels/Lookups/PriceGroupsViewModel.cs`
- `src/HardwarePaintShop.Desktop/ViewModels/Lookups/ExpenseCategoriesViewModel.cs`
- `src/HardwarePaintShop.Desktop/Views/Lookups/UnitsView.xaml`
- `src/HardwarePaintShop.Desktop/Views/Lookups/PriceGroupsView.xaml`
- `src/HardwarePaintShop.Desktop/Views/Lookups/ExpenseCategoriesView.xaml`
- `src/HardwarePaintShop.Desktop/ViewModels/Dashboard/DashboardViewModel.cs`
- `src/HardwarePaintShop.Desktop/Views/Dashboard/DashboardView.xaml`

## Files that should be created

- EF Core migration files under `src/HardwarePaintShop.Infrastructure/Migrations/` only after the EF model is corrected.
- Seed/initializer code or SQL for default admin, permissions, and role permissions.
- Focused unit tests under `tests/HardwarePaintShop.UnitTests/`.

## Acceptance criteria

- `dotnet restore HardwarePaintShopSystem.sln` succeeds.
- `dotnet build HardwarePaintShopSystem.sln --no-restore` succeeds with 0 errors.
- Application no longer depends directly on Infrastructure, or the architecture is intentionally changed and project references match.
- DI can resolve `LoginWindow`, `MainWindow`, `LoginViewModel`, `MainViewModel`, lookup ViewModels, user/role ViewModels, auth, permissions, lookup, and user services.
- Lookup views bind to properties/commands that exist.
- Dashboard refresh command binding matches the generated command name.
- Database model strategy is explicit: EF migrations are generated from the corrected model, or SQL schema use is documented and EF model is aligned to it.
- A fresh database has a valid BCrypt-hashed default admin and permissions.

## Build and test commands

```text
dotnet --info
dotnet restore HardwarePaintShopSystem.sln
dotnet build HardwarePaintShopSystem.sln --no-restore
dotnet test HardwarePaintShopSystem.sln --no-build
```

## Risks

- Fixing the Application/Infrastructure boundary may require moving service implementations between projects.
- Aligning EF entities with the SQL schema may change many property names and relationships.
- Creating migrations before resolving schema/model conflicts could lock in an incorrect database model.
- Seeding admin credentials must avoid committing real passwords; store only a BCrypt hash or use a first-run initializer.

## Things the next agent must not change

- Do not add product/sales/purchase/customer feature screens before the solution builds.
- Do not commit real database passwords.
- Do not delete the existing planning docs or SQL schema without explicit approval.
- Do not bypass the stock and cash movement design rules documented in `docs/implementation_plan.md`.
- Do not introduce a second ORM or database provider.
