# Version 0.2 — Products Module

## Delivered

- Products page connected to the main navigation.
- Create and edit a product with category, base unit, reorder level, notes, serial tracking, active status, and image.
- Soft deactivation/reactivation instead of product deletion.
- Multiple product units with conversion factors and default purchase/sale units.
- Multiple prices by unit and price group.
- Multiple barcodes, optionally associated with a specific product unit.
- Search in the products page by name, code, barcode, and serial number.
- Separate price inquiry page with price-group filtering and Enter/barcode-scanner support.
- Current stock shown from the sum of `StockMovement.QuantityBaseUnit`; stock is never stored directly on the product.
- Product permission codes seeded and granted to the Owner role.
- Focused unit tests for the new view models and application request model.

## Important Data Rules

- The base product unit always has a conversion factor of `1`.
- Product code and barcode values cannot be reused by another product.
- A unit/price-group combination can have only one price.
- The minimum sale price cannot be greater than the sale price.
- A serial-tracked product cannot have serial tracking switched off after serial records exist.
- Removing a product unit deactivates it rather than deleting historical unit records.
- Product saving, units, prices, and barcodes are committed in one database transaction.

## Serial Number Scope

Version 0.2 stores whether a product requires serial tracking and searches the existing `ProductSerial` table. Creating serial-number instances belongs to Purchase Invoices / Stock In, because a serial represents a physical item entering inventory. This avoids creating serials without a matching stock movement.

## Image Storage

Selected product images are copied to:

```text
%LOCALAPPDATA%\HardwarePaintShop\Images\Products
```

The database stores the copied image path, not the original source file path.

## Verification on Windows

From the solution folder:

```powershell
dotnet restore HardwarePaintShopSystem.sln
dotnet build HardwarePaintShopSystem.sln --no-restore
dotnet test HardwarePaintShopSystem.sln --no-build
dotnet run --project src\HardwarePaintShop.Desktop\HardwarePaintShop.Desktop.csproj
```

No new migration command is required for Version 0.2. `DatabaseInitializer` will add the new permission records and grant them to the Owner role when the application starts.

## Suggested Manual Test

1. Sign in as the Owner/admin user.
2. Ensure at least one category, unit, and price group exists.
3. Add a product with a base unit, another converted unit, a price, and a barcode.
4. Edit the product and verify all child rows reload correctly.
5. Find it from Products by name, code, and barcode.
6. Open Price Inquiry, scan/type the barcode, and press Enter.
7. Deactivate the product, enable “show inactive,” then reactivate it.
