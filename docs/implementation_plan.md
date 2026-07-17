# Hardware & Paint Shop Management System — Implementation Plan

> **Prepared:** 2026-06-07  
> **Stack:** C# .NET 8 WPF · PostgreSQL · Entity Framework Core 8 · MVVM · ASP.NET Core Web API (Phase 6) · Flutter (Phase 7)

---

## 1. Project Overview

A complete shop-management system for a hardware and paint store.  
The system replaces manual ledgers with a single source of truth covering:
inventory, purchasing, sales, accounting, cashbox, returns, alerts, printing, and backup.

**Core architectural rules that must never be violated:**
1. Inventory quantity is **never edited directly** — only via `stock_movements`.
2. Invoices are **never deleted** — voided with `status = void` + audit trail.
3. The cashbox balance is derived from `cash_movements`, not from a single editable field.
4. Every financial or stock event creates a traceable ledger row.

---

## 2. Schema Review & Identified Issues

### 2.1 What the Schema Gets Right ✅

| Area | Verdict |
|---|---|
| UUID primary keys with `gen_random_uuid()` | ✅ Good for future distributed use |
| `stock_movements` as the single ledger for inventory | ✅ Correct pattern |
| `cash_movements` for cashbox traceability | ✅ Correct pattern |
| `customer_transactions` / `supplier_transactions` for AR/AP | ✅ Complete double-entry trail |
| `product_units` with `conversion_factor_to_base` | ✅ Handles buy-bag/sell-kg perfectly |
| `product_prices` per `(product_unit, price_group)` | ✅ Multi-price-group support |
| `product_costs` with `average_cost_base_unit` | ✅ Needed for profit reports |
| `product_barcodes` (one product → many barcodes per unit) | ✅ Correct |
| `product_serials` with status lifecycle | ✅ Correct |
| `returns` + `return_items` as a dedicated module | ✅ Correct |
| Soft-delete via `is_active` everywhere | ✅ Correct |
| `backup_logs` / `sync_devices` / `sync_logs` | ✅ Foundation present |

---

### 2.2 Schema Issues & Missing Items ⚠️

#### Issue 1 — `product_serials` has dangling FK references
`purchase_invoice_id` and `sales_invoice_id` are `UUID` columns with **no FK constraint** declared.  
**Fix:** Add explicit FK constraints referencing `purchase_invoices(id)` and `sales_invoices(id)`.

```sql
ALTER TABLE product_serials
  ADD CONSTRAINT fk_serial_purchase FOREIGN KEY (purchase_invoice_id) REFERENCES purchase_invoices(id),
  ADD CONSTRAINT fk_serial_sale     FOREIGN KEY (sales_invoice_id)    REFERENCES sales_invoices(id);
```

#### Issue 2 — `returns.original_invoice_id` has no FK type or reference
Cannot tell if it references `sales_invoices` or `purchase_invoices` at the DB level.  
**Fix:** Add a `reference_type VARCHAR(50)` + keep `original_invoice_id` as a loose UUID (polymorphic pattern), or split into two nullable FK columns:

```sql
ALTER TABLE returns
  ADD COLUMN original_sales_invoice_id     UUID REFERENCES sales_invoices(id),
  ADD COLUMN original_purchase_invoice_id  UUID REFERENCES purchase_invoices(id);
```

#### Issue 3 — No `owner_withdrawals` table
Owner withdrawals should be a first-class entity to support dedicated reporting (section 21 of spec).  
Currently mixed into `cash_movements.movement_type = 'owner_withdrawal'`.  
**Recommendation:** Add a dedicated table for auditing purposes:

```sql
CREATE TABLE owner_withdrawals (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cashbox_id UUID NOT NULL REFERENCES cashboxes(id),
    amount NUMERIC(14,2) NOT NULL CHECK (amount >= 0),
    withdrawal_date DATE NOT NULL DEFAULT CURRENT_DATE,
    user_id UUID REFERENCES users(id),
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

#### Issue 4 — No `inventory_counts` (جرد) table
Physical stock-count sessions are mentioned prominently but have no dedicated table.  
Stock adjustments from a count are recorded in `stock_movements` (correct), but the count session itself needs a header record.

```sql
CREATE TABLE inventory_counts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    count_no VARCHAR(80) NOT NULL UNIQUE,
    count_scope VARCHAR(50) NOT NULL DEFAULT 'full', -- full, category, product
    status VARCHAR(50) NOT NULL DEFAULT 'draft', -- draft, confirmed, cancelled
    user_id UUID REFERENCES users(id),
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    confirmed_at TIMESTAMP
);

CREATE TABLE inventory_count_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    count_id UUID NOT NULL REFERENCES inventory_counts(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id),
    system_qty_base NUMERIC(14,3) NOT NULL,
    actual_qty_base NUMERIC(14,3) NOT NULL,
    difference_base NUMERIC(14,3) GENERATED ALWAYS AS (actual_qty_base - system_qty_base) STORED,
    notes TEXT
);
```

#### Issue 5 — No `app_settings` table
The spec requires configurable backup paths, invoice header, printer type (58mm/80mm/A4), overdue-invoice threshold days etc.

```sql
CREATE TABLE app_settings (
    key VARCHAR(150) PRIMARY KEY,
    value TEXT,
    description TEXT,
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

#### Issue 6 — No `invoice_payments` table
A single invoice may receive multiple partial payments over time (common for credit invoices).  
Currently only `paid_amount` on the invoice header is tracked.  
**Fix:** Add a payments table to record each collection event against a specific invoice.

```sql
CREATE TABLE invoice_payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_no VARCHAR(80) NOT NULL UNIQUE,
    payment_type VARCHAR(20) NOT NULL CHECK (payment_type IN ('sales', 'purchase')),
    sales_invoice_id UUID REFERENCES sales_invoices(id),
    purchase_invoice_id UUID REFERENCES purchase_invoices(id),
    customer_id UUID REFERENCES customers(id),
    supplier_id UUID REFERENCES suppliers(id),
    cashbox_id UUID REFERENCES cashboxes(id),
    amount NUMERIC(14,2) NOT NULL CHECK (amount > 0),
    payment_date DATE NOT NULL DEFAULT CURRENT_DATE,
    user_id UUID REFERENCES users(id),
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

#### Issue 7 — No `audit_logs` table
The spec requires tracking "who changed what and when" (prices, quantities, voided invoices).

```sql
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    table_name VARCHAR(100) NOT NULL,
    record_id UUID NOT NULL,
    action VARCHAR(20) NOT NULL CHECK (action IN ('INSERT','UPDATE','DELETE','VOID')),
    changed_by UUID REFERENCES users(id),
    old_values JSONB,
    new_values JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

#### Issue 8 — `alerts` has no `user_id` (recipient)
Alerts reference a record but don't specify which user should see them.  
**Fix:** Add `recipient_role_id UUID REFERENCES roles(id)` or `user_id UUID REFERENCES users(id)`.

#### Issue 9 — `cashboxes.current_balance` is redundant (dual-write risk)
The spec says "cashbox balance is computed from movements."  
Storing `current_balance` as a live column risks inconsistency unless updated transactionally every time.  
**Recommendation:** Keep it as a **cached/denormalized** field, updated only by trigger or service, with a comment in schema noting this.

---

### 2.3 Missing Seed Data
The schema seeds price groups, units, expense categories, roles, and one cashbox — but does **not** seed:
- Default permissions per role (the `permissions` and `role_permissions` tables are empty).
- A default `Owner` user (the system cannot be used until one exists).

**Phase 1 supplemental SQL will add these.**

---

## 3. Milestones

### MILESTONE 0 — Infrastructure & Project Setup *(1–2 days)*
- Create WPF solution with correct folder structure.
- Install NuGet packages: EF Core, Npgsql, CommunityToolkit.Mvvm, Material Design (or FluentUI), etc.
- Configure `appsettings.json` (connection string, app settings).
- Apply Phase 01 SQL schema + supplemental fixes to local PostgreSQL.
- Set up EF Core `DbContext` and initial migrations.
- Implement login screen + session/auth service.
- Implement role/permission check helper.

---

### MILESTONE 1 — Lookup & Master Data *(2–3 days)*
Screens and CRUD for:
- Categories (حسب التصنيف الشجري — parent/child).
- Units (الوحدات).
- Price Groups (فئات الأسعار).
- Expense Categories (أنواع المصروفات).
- Roles & Permissions management.
- Users management.

---

### MILESTONE 2 — Products *(3–4 days)*
- Product list with search (name, code, barcode, serial).
- Add/Edit product dialog: basic info, category, base unit, supplier, image, min stock, serial-tracked flag.
- Product units tab: add units with conversion factors, mark default purchase/sale.
- Product prices tab: price per unit per price group.
- Product barcodes tab: add/scan barcodes per unit.
- Product serials tab: register serials, view status/history.
- Price inquiry screen (استعلام سعر) — available to Cashier.

---

### MILESTONE 3 — Customers & Suppliers *(2 days)*
- Customer list + add/edit + credit limit + price group assignment.
- Supplier list + add/edit.
- Customer account screen: balance, transactions, statement printing.
- Supplier account screen: balance, transactions, statement printing.

---

### MILESTONE 4 — Purchase Invoices *(3 days)*
- Purchase invoice list.
- Create purchase invoice: select supplier, add items (unit, qty, cost), extra costs, totals.
- On save:
  - Insert `purchase_invoice` + `purchase_invoice_items`.
  - Insert `stock_movements` (IN) for each item.
  - Update `product_costs.last_purchase_price` + recalculate `average_cost_base_unit`.
  - Insert `supplier_transactions` (purchase debit).
  - If paid: insert `cash_movements` (OUT) + update `cashboxes.current_balance`.
- Void invoice with reason (reversal movements auto-created).
- Register serial numbers from purchase.

---

### MILESTONE 5 — Sales Invoices *(4 days)*
- Sales invoice list.
- POS-style create invoice screen:
  - Select/search customer → auto-load price group.
  - Add items by name / barcode scan / serial scan.
  - Choose unit, qty, price (auto from price group), discount per line.
  - Summary: subtotal, discount, total, paid, remaining.
  - Payment type: cash, credit, partial.
- On save:
  - Insert `sales_invoice` + `sales_invoice_items`.
  - Insert `stock_movements` (OUT) for each item.
  - Update serial status to `sold` if applicable.
  - If credit limit exceeded → warning/block by permission.
  - Insert `customer_transactions` (sale debit).
  - If paid: insert `cash_movements` (IN) + update cashbox.
- Void invoice.

---

### MILESTONE 6 — Collections & Payments *(2 days)*
- Collect from customer (customer account → record payment):
  - Creates `customer_transactions` (payment credit).
  - Creates `cash_movements` (IN).
  - Updates `invoice_payments` (links payment to invoice(s)).
- Pay supplier:
  - Creates `supplier_transactions` (payment debit).
  - Creates `cash_movements` (OUT).

---

### MILESTONE 7 — Cashbox & Expenses *(2 days)*
- Cashbox screen: balance summary, movements list.
- Add expense: select category, amount, cashbox, notes.
- Owner withdrawal: amount, notes (movement_type = `owner_withdrawal`).
- Owner deposit: amount, notes (movement_type = `owner_deposit`).

---

### MILESTONE 8 — Returns *(3 days)*
- Sales return: link to original invoice (optional), select items/qty, condition (good/damaged), refund method (cash / reduce balance).
- Purchase return: link to original invoice, reduce supplier balance or get cash back.
- Each return creates corresponding `stock_movements` and `cash_movements` / `customer_transactions` / `supplier_transactions`.

---

### MILESTONE 9 — Inventory & Stock Count *(2 days)*
- Current stock report (from `product_stock_balances` view).
- Stock movement history per product.
- New inventory count session: choose scope → enter actual qty per product → confirm → auto-create adjustment `stock_movements`.
- Low-stock alert generation.

---

### MILESTONE 10 — Alerts *(1 day)*
- Background service (timer) checks:
  - Products below `min_stock_base_qty`.
  - Customers over `credit_limit`.
  - Unpaid invoices older than configured threshold days.
  - Missing backup today.
- Alert bell icon in main nav with unread count.
- Alerts list screen with mark-as-read.

---

### MILESTONE 11 — Dashboard *(2 days)*
- Today's sales total, collections, expenses.
- Current cashbox balance.
- Invoice count (sales & purchases) today.
- Low-stock product list.
- Overdue invoice list.
- Customers over credit limit.
- Top 5 selling products (this month).
- Recent cashbox movements.
- Recent sales invoices.

---

### MILESTONE 12 — Reports & Printing *(3–4 days)*
- Sales reports: by date range, by product, by customer, by user.
- Purchase reports: by date range, by supplier.
- Customer statements (A4 + thermal).
- Supplier statements (A4).
- Stock report: current balances, movement history.
- Cashbox report: daily summary.
- Thermal invoice printing (58mm or 80mm — configurable).
- A4 invoice printing.
- Returns report.

---

### MILESTONE 13 — Backup & Restore *(1–2 days)*
- Manual backup: `pg_dump` to ZIP with timestamp.
- Auto-backup: Windows Task Scheduler or WPF background timer at configured time.
- Optional USB / network path copy.
- Restore: select backup file → pre-restore auto-backup → `pg_restore` → restart app.
- `backup_logs` updated for every attempt.

---

### MILESTONE 14 — Settings & Polish *(1–2 days)*
- App settings screen: shop name, address, phone, logo, printer type, overdue days, backup schedule, backup path.
- Theme / language settings.
- About screen.
- Full permission enforcement QA pass.
- Performance review (EF query optimizations, indexes).

---

### MILESTONE 15 — ASP.NET Core Web API *(3–4 days)*
- Separate project in solution.
- JWT authentication (shared user table).
- Endpoints for: auth, products, customers, stock-adjustment, alerts, payments, sync.
- Middleware: permission check, rate limiting.

---

### MILESTONE 16 — Flutter Mobile App *(separate sprint)*
- Covered in spec sections 29–30; to be planned separately after API is stable.

---

## 4. Technology Decisions

| Technology | Choice | Reason |
|---|---|---|
| UI Framework | WPF (.NET 8) | Windows-native, strong printing |
| MVVM Toolkit | CommunityToolkit.Mvvm | Source-generator based, minimal boilerplate |
| UI Controls | MahApps.Metro or Material Design In XAML | Modern WPF look |
| ORM | EF Core 8 + Npgsql | Full PostgreSQL support, migrations |
| Database | PostgreSQL 16 | Robust, multi-user, great for reporting |
| Barcode Scan | Raw HID keyboard wedge (scanner types as text) | No extra library needed |
| Barcode Generate/Print | ZXing.Net.Bindings | Free, integrates with WPF |
| Thermal Printing | RawPrint / `ESCPOS.NET` | Direct ESC/POS command support |
| A4 Printing | WPF PrintDialog + FlowDocument | Native |
| Backup | `pg_dump.exe` called via `Process` | Reliable PostgreSQL native |
| Dependency Injection | Microsoft.Extensions.DependencyInjection | Standard .NET |
| Logging | Serilog → file + DB | Audit + debug |
| Config | `appsettings.json` + EF `app_settings` table | Hybrid |

---

## 5. Development Principles

1. **No direct stock edits.** All quantity changes go through a `StockMovementService`.
2. **No direct cashbox edits.** All cash flows go through a `CashMovementService`.
3. **Transactional saves.** Invoice creation is a single DB transaction covering all side effects.
4. **Permission checks everywhere.** Each ViewModel checks `IPermissionService.Can(permissionCode)` before exposing commands.
5. **Void, never delete.** Invoice and adjustment reversal methods auto-create compensating records.
6. **Audit every change.** `AuditService.Log()` is called by services for sensitive mutations.

---

## 6. Testing Strategy

- **Unit tests:** Service-layer logic (unit conversion, pricing, totals).
- **Integration tests:** EF Core against a local test PostgreSQL instance.
- **Manual QA checklist** per milestone before marking done.
