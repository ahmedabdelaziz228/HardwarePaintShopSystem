# Version 0.5 — Sales POS & Stock Out

## UI Hotfix

The custom TextBox template was removed because it could receive focus while failing to render typed text on some Windows/WPF environments. The canonical input style now uses the native WPF template with explicit text, caret, selection, focus, read-only, and disabled colors. This fixes all screens that use `AppTextBoxStyle`, including Products.

## Sales POS

- Search by barcode, product code, name, or serial number.
- Automatically add an exact/single search result to the cart.
- Sell using any active product unit and convert to base stock automatically.
- Select a price group, with the customer's configured group applied automatically.
- Editable quantities and prices, with minimum-price permission enforcement.
- Invoice-level discount and live subtotal/net/remaining calculations.
- Cash customer or registered customer.
- Cash, credit, or partial payment.
- Customer credit-limit enforcement before posting.
- Multiple cashboxes and payment methods.
- Serial entry and availability validation.
- Serial reservation while a sales draft is open.
- F2 starts a new invoice; F9 posts the current invoice.
- Recent invoice browser and controlled draft cancellation.

## Atomic Posting

Posting a sale commits all of the following together, or none of them:

- sales invoice state and payment status;
- negative stock movements;
- customer debit/payment ledger and current balance;
- invoice payment;
- cash-in movement and cashbox balance;
- serial status from Reserved to Sold;
- audit log.

## Safety Rules

- A cash customer must pay the full invoice.
- Credit or partial sales require a registered active customer.
- The projected customer balance may not exceed the credit limit.
- Stock cannot become negative.
- Serial-tracked products are sold using their base piece unit with one serial per piece.
- Posted sales are not destructively voided; they will be handled through Version 0.7 returns.

## Database

No new migration is required. Version 0.5 uses the `DueDate` and `PaymentMethod` fields introduced by the Version 0.4 migration.
