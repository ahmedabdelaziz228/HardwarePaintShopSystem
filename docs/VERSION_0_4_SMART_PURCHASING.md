# Version 0.4 — Smart Purchasing & Stock In

## Delivered

- Modern purchase workspace with invoice browser and inline editor.
- Draft, edit, final post, draft void, and controlled posted-invoice reversal.
- Internal invoice numbering plus optional supplier invoice number.
- Duplicate supplier-invoice protection per supplier.
- Invoice and due dates.
- Product search by name, code, barcode, or registered serial.
- Purchase in any active product unit with a visible conversion to the base unit.
- Serial entry for serial-tracked products, exact quantity validation, and global duplicate checks.
- Shipping and landed costs allocated proportionally across invoice lines.
- Last purchase cost and weighted-average base-unit cost updates.
- One atomic database transaction for:
  - stock movements;
  - product cost;
  - supplier ledger and current balance;
  - optional invoice payment;
  - cashbox movement and balance;
  - serial status;
  - audit log.
- Payment methods: cash, card, bank transfer, mobile wallet, InstaPay, and other.
- Multiple cashboxes, opening balances, safe activation/deactivation, and no direct balance editing.
- A default main cashbox is created automatically on initialization.
- Product main supplier and a richer price-inquiry screen with category, supplier, compound stock, and permission-protected last cost.

## Safety Rules

- Only draft purchases can be edited or posted.
- A paid posted purchase cannot be voided directly; it will require the Version 0.7 purchase-return workflow.
- A posted unpaid purchase can be reversed only while sufficient stock remains and all of its serials are still available.
- Cash payments require an active cashbox with a sufficient balance.
- Cashbox opening balance can be set only when the cashbox is created.
- A non-zero cashbox cannot be deactivated.
- Product stock is never edited directly; it is derived from stock movements.

## Database Upgrade

Migration `20260717000100_AddPurchasingFields` adds:

- `PurchaseInvoices.SupplierInvoiceNo`;
- `PurchaseInvoices.DueDate`;
- `SalesInvoices.DueDate` for the upcoming POS credit workflow;
- `InvoicePayments.PaymentMethod`;
- a unique supplier/external-invoice index.

The desktop initializer calls `Database.MigrateAsync()`, so the migration is applied at startup with the configured PostgreSQL connection.

## Next Recommended Version

Version 0.5 should implement the barcode-first Sales POS and stock-out workflow, reusing the same atomic transaction pattern introduced here.
