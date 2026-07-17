# Version 0.3 — Customers, Suppliers & UI Refresh

## Delivered

- Customer list, search by name/phone/address, add, edit, activate, and deactivate.
- Customer type, price group, credit limit, current receivables, address, phone, and notes.
- Supplier list, search by name/phone/address, add, edit, activate, and deactivate.
- Supplier current payables, address, phone, and notes.
- Customer and supplier permission codes seeded and automatically granted to the Owner role.
- Real dashboard data from PostgreSQL instead of placeholder zero values.
- Low-stock overview calculated from stock movements and each product's reorder level.
- Modernized login, sidebar, active-navigation state, cards, inputs, buttons, data grids, and colors.
- Future modules are visibly marked “coming soon” and disabled until their business rules are implemented.
- Desktop assembly version set to `0.3.0`.

## Accounting Safety

The Customer and Supplier screens never edit `CurrentBalance`. Customer receivables and supplier payables will be changed only by invoice/payment transaction flows. This prevents manual edits from breaking the accounting ledger.

## Dashboard Values

The dashboard now reads:

- Today's active sales total and invoice count.
- Today's customer collections and sale payments.
- Today's expenses.
- Active cashbox balances.
- Customer receivables and supplier payables.
- Active product, customer, and supplier counts.
- Products at or below their minimum stock level.

## Verification on Windows

```powershell
dotnet restore HardwarePaintShopSystem.sln
dotnet build HardwarePaintShopSystem.sln --no-restore
dotnet test HardwarePaintShopSystem.sln --no-build
dotnet run --project src\HardwarePaintShop.Desktop\HardwarePaintShop.Desktop.csproj
```

No new EF migration is required. Start the application once so `DatabaseInitializer` can seed the new permission codes.
