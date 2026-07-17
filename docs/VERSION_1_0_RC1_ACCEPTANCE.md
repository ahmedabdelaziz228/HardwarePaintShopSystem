# Version 1.0 RC1 acceptance checklist

## Required environment

- Windows 10/11 x64
- PostgreSQL 14 or later
- .NET SDK 8 for source builds (not required by the self-contained published app)
- Inno Setup 6 only when building the installer
- A disposable PostgreSQL database for restore tests

## Build gate

```powershell
dotnet restore .\HardwarePaintShopSystem.sln
dotnet build .\HardwarePaintShopSystem.sln -c Release --no-restore
dotnet test .\HardwarePaintShopSystem.sln -c Release --no-build
```

Do not publish if any command fails. Send the full compiler output, including file path and line number, for correction.

## Functional gate

1. Create product units, prices, two barcodes, and a serial-tracked product.
2. Post a purchase with extra costs, partial supplier payment, and serials.
3. Verify stock, weighted cost, supplier balance, cashbox, and serial availability.
4. Post cash, credit, and partial sales; verify customer limits and negative-stock blocking.
5. Print an 80 mm receipt and repeat with the 58 mm setting.
6. Post partial sales and purchase returns; verify maximum quantities, stock, serials, cash, and party balances.
7. Record an expense, customer collection, supplier payment, owner movement, and cash transfer.
8. Run a physical count with one positive and one negative difference.
9. Refresh alerts and compare low stock and overdue invoices with source data.
10. Export and print a period report; reconcile totals against invoices and cash movements.
11. Create a manual backup, add temporary data, restore the backup, restart, and verify the temporary data is gone.
12. Generate a signed license for the displayed machine ID and activate it.

## Release gate

- Change the default admin password.
- Store the seller private license key outside the customer package.
- Confirm the customer-specific shop header, tax number, receipt width, and backup path.
- Keep a tested off-device backup.
- Record the installed app version and database backup date on the handover sheet.
