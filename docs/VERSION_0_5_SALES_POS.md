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
- Cash customer or registered customer; a debtor customer is created automatically only when a remaining balance exists.
- Cash, credit, or partial payment.
- Customer credit-limit enforcement before posting and dated customer account statements.
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

## Invoice and profitability

- A4, 80 mm, and 58 mm detailed invoices include unit, quantity, actual unit price, totals, paid amount, remaining amount, and customer balance snapshots.
- Shop/legal data, logo, title, footer, currency, and paper size are editable in Settings.
- Microsoft Print to PDF provides PDF output without an extra runtime dependency.
- Each posted line captures its weighted-average cost and gross profit, so later purchase-price changes do not rewrite historical profit.
- Sales returns reverse both revenue and captured cost in the business report.

## Safety Rules

- A fully paid cash invoice never creates a customer automatically.
- Credit or partial sales require an existing customer or the name of a customer to create atomically.
- The projected customer balance may not exceed the credit limit.
- Stock cannot become negative.
- Serial-tracked products are sold using their base piece unit with one serial per piece.
- Posted sales are not destructively voided; they will be handled through Version 0.7 returns.

## Database

Migration `20260718000100_AddSalesProfitAndBalanceSnapshots` adds historical sales cost/profit and customer balance snapshots. Standard piece, kilogram, gram, meter, centimeter, bag, roll, and liter units are seeded safely when missing.
