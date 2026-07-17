-- ============================================================
-- Shop Management System - Phase 01 Database Schema
-- نظام إدارة محل حدايد وبوهيات - المرحلة الأولى: قاعدة البيانات
-- Database: PostgreSQL
-- ============================================================

-- ملاحظات مهمة:
-- 1) كل الكميات يتم تخزينها بالوحدة الأساسية للمنتج داخل stock_movements.
-- 2) لا يتم تعديل المخزون مباشرة؛ أي تغيير يتم من خلال stock_movements.
-- 3) لا يتم حذف الفواتير؛ يتم إلغاؤها status = void.
-- 4) الخزينة تعتمد على cash_movements وليس تعديل الرصيد يدويًا فقط.
-- 5) الباركود يخص المنتج/الوحدة، أما السيريال يخص قطعة واحدة فريدة.

-- ============================================================
-- 00. Extensions
-- ============================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- ============================================================
-- 01. Common lookup tables
-- ============================================================

CREATE TABLE categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(150) NOT NULL UNIQUE,
    parent_id UUID REFERENCES categories(id),
    notes TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE units (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE, -- كيلو، شكارة، جردل، لتر، قطعة
    symbol VARCHAR(30),
    notes TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE price_groups (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE, -- قطاعي، جملة، مقاول، تاجر
    description TEXT,
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE expense_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE, -- إيجار، كهرباء، نقل، عمالة
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- ============================================================
-- 02. Users and permissions
-- ============================================================

CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE, -- Owner, Cashier, StoreKeeper
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    full_name VARCHAR(150) NOT NULL,
    username VARCHAR(100) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    role_id UUID NOT NULL REFERENCES roles(id),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(150) NOT NULL UNIQUE, -- مثال: products.create, sales.refund
    name VARCHAR(150) NOT NULL,
    description TEXT
);

CREATE TABLE role_permissions (
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id UUID NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);

-- ============================================================
-- 03. Customers and suppliers
-- ============================================================

CREATE TABLE customers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    phone VARCHAR(50),
    address TEXT,
    price_group_id UUID REFERENCES price_groups(id),
    credit_limit NUMERIC(14,2) NOT NULL DEFAULT 0,
    opening_balance NUMERIC(14,2) NOT NULL DEFAULT 0,
    notes TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_customers_name ON customers(name);
CREATE INDEX idx_customers_phone ON customers(phone);

CREATE TABLE suppliers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    phone VARCHAR(50),
    address TEXT,
    opening_balance NUMERIC(14,2) NOT NULL DEFAULT 0,
    notes TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_suppliers_name ON suppliers(name);
CREATE INDEX idx_suppliers_phone ON suppliers(phone);

-- ============================================================
-- 04. Products, units, prices, barcodes, serials
-- ============================================================

CREATE TABLE products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(100) UNIQUE, -- كود داخلي
    name VARCHAR(250) NOT NULL,
    category_id UUID REFERENCES categories(id),
    base_unit_id UUID NOT NULL REFERENCES units(id), -- الوحدة الأساسية: كيلو، لتر، قطعة
    image_path TEXT,
    default_supplier_id UUID REFERENCES suppliers(id),
    min_stock_base_qty NUMERIC(14,3) NOT NULL DEFAULT 0,
    is_serial_tracked BOOLEAN NOT NULL DEFAULT FALSE,
    notes TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_products_name ON products(name);
CREATE INDEX idx_products_code ON products(code);

CREATE TABLE product_units (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    unit_id UUID NOT NULL REFERENCES units(id),
    conversion_factor_to_base NUMERIC(14,6) NOT NULL, -- مثال: شكارة = 25 كيلو
    is_default_purchase BOOLEAN NOT NULL DEFAULT FALSE,
    is_default_sale BOOLEAN NOT NULL DEFAULT FALSE,
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE(product_id, unit_id)
);

CREATE TABLE product_prices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    product_unit_id UUID NOT NULL REFERENCES product_units(id) ON DELETE CASCADE,
    price_group_id UUID NOT NULL REFERENCES price_groups(id),
    sale_price NUMERIC(14,2) NOT NULL DEFAULT 0,
    min_sale_price NUMERIC(14,2) NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE(product_id, product_unit_id, price_group_id)
);

CREATE TABLE product_costs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    product_unit_id UUID NOT NULL REFERENCES product_units(id) ON DELETE CASCADE,
    last_purchase_price NUMERIC(14,2) NOT NULL DEFAULT 0,
    average_cost_base_unit NUMERIC(14,6) NOT NULL DEFAULT 0,
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE(product_id, product_unit_id)
);

CREATE TABLE product_barcodes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    product_unit_id UUID REFERENCES product_units(id),
    barcode VARCHAR(150) NOT NULL UNIQUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE product_serials (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    serial_number VARCHAR(200) NOT NULL UNIQUE,
    status VARCHAR(50) NOT NULL DEFAULT 'available', -- available, sold, returned, damaged
    purchase_invoice_id UUID,
    sales_invoice_id UUID,
    customer_id UUID REFERENCES customers(id),
    warranty_until DATE,
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    sold_at TIMESTAMP
);

CREATE INDEX idx_product_serials_serial ON product_serials(serial_number);

-- ============================================================
-- 05. Cashboxes and money movements
-- ============================================================

CREATE TABLE cashboxes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(150) NOT NULL UNIQUE, -- الخزينة الرئيسية، خزينة الكاشير
    opening_balance NUMERIC(14,2) NOT NULL DEFAULT 0,
    current_balance NUMERIC(14,2) NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE cash_movements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cashbox_id UUID NOT NULL REFERENCES cashboxes(id),
    direction VARCHAR(10) NOT NULL CHECK (direction IN ('in', 'out')),
    movement_type VARCHAR(80) NOT NULL, -- sale_payment, expense, supplier_payment, owner_withdrawal
    amount NUMERIC(14,2) NOT NULL CHECK (amount >= 0),
    reference_type VARCHAR(80),
    reference_id UUID,
    user_id UUID REFERENCES users(id),
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_cash_movements_date ON cash_movements(created_at);
CREATE INDEX idx_cash_movements_type ON cash_movements(movement_type);

CREATE TABLE expenses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    expense_category_id UUID NOT NULL REFERENCES expense_categories(id),
    cashbox_id UUID NOT NULL REFERENCES cashboxes(id),
    amount NUMERIC(14,2) NOT NULL CHECK (amount >= 0),
    expense_date DATE NOT NULL DEFAULT CURRENT_DATE,
    notes TEXT,
    user_id UUID REFERENCES users(id),
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- ============================================================
-- 06. Sales invoices
-- ============================================================

CREATE TABLE sales_invoices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_no VARCHAR(80) NOT NULL UNIQUE,
    customer_id UUID REFERENCES customers(id),
    price_group_id UUID REFERENCES price_groups(id),
    invoice_date TIMESTAMP NOT NULL DEFAULT NOW(),
    subtotal NUMERIC(14,2) NOT NULL DEFAULT 0,
    discount_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    total_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    paid_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    remaining_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    payment_status VARCHAR(50) NOT NULL DEFAULT 'unpaid', -- paid, partial, unpaid
    status VARCHAR(50) NOT NULL DEFAULT 'active', -- active, void, returned
    cashbox_id UUID REFERENCES cashboxes(id),
    user_id UUID REFERENCES users(id),
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_sales_invoices_no ON sales_invoices(invoice_no);
CREATE INDEX idx_sales_invoices_customer ON sales_invoices(customer_id);
CREATE INDEX idx_sales_invoices_date ON sales_invoices(invoice_date);

CREATE TABLE sales_invoice_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sales_invoice_id UUID NOT NULL REFERENCES sales_invoices(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id),
    product_unit_id UUID NOT NULL REFERENCES product_units(id),
    quantity NUMERIC(14,3) NOT NULL CHECK (quantity > 0),
    quantity_base_unit NUMERIC(14,3) NOT NULL CHECK (quantity_base_unit > 0),
    unit_price NUMERIC(14,2) NOT NULL,
    discount_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    total_amount NUMERIC(14,2) NOT NULL,
    serial_id UUID REFERENCES product_serials(id),
    notes TEXT
);

-- ============================================================
-- 07. Purchase invoices
-- ============================================================

CREATE TABLE purchase_invoices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_no VARCHAR(80) NOT NULL UNIQUE,
    supplier_id UUID NOT NULL REFERENCES suppliers(id),
    invoice_date TIMESTAMP NOT NULL DEFAULT NOW(),
    subtotal NUMERIC(14,2) NOT NULL DEFAULT 0,
    extra_costs NUMERIC(14,2) NOT NULL DEFAULT 0, -- نقل، تحميل، إلخ
    discount_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    total_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    paid_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    remaining_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    payment_status VARCHAR(50) NOT NULL DEFAULT 'unpaid',
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    cashbox_id UUID REFERENCES cashboxes(id),
    user_id UUID REFERENCES users(id),
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_purchase_invoices_supplier ON purchase_invoices(supplier_id);
CREATE INDEX idx_purchase_invoices_date ON purchase_invoices(invoice_date);

CREATE TABLE purchase_invoice_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    purchase_invoice_id UUID NOT NULL REFERENCES purchase_invoices(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id),
    product_unit_id UUID NOT NULL REFERENCES product_units(id),
    quantity NUMERIC(14,3) NOT NULL CHECK (quantity > 0),
    quantity_base_unit NUMERIC(14,3) NOT NULL CHECK (quantity_base_unit > 0),
    unit_cost NUMERIC(14,2) NOT NULL,
    total_cost NUMERIC(14,2) NOT NULL,
    notes TEXT
);

-- ============================================================
-- 08. Customer and supplier accounting movements
-- ============================================================

CREATE TABLE customer_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id UUID NOT NULL REFERENCES customers(id),
    transaction_type VARCHAR(80) NOT NULL, -- opening, sale, payment, sales_return, adjustment
    direction VARCHAR(10) NOT NULL CHECK (direction IN ('debit', 'credit')),
    amount NUMERIC(14,2) NOT NULL CHECK (amount >= 0),
    reference_type VARCHAR(80),
    reference_id UUID,
    user_id UUID REFERENCES users(id),
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_customer_transactions_customer ON customer_transactions(customer_id);
CREATE INDEX idx_customer_transactions_date ON customer_transactions(created_at);

CREATE TABLE supplier_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    supplier_id UUID NOT NULL REFERENCES suppliers(id),
    transaction_type VARCHAR(80) NOT NULL, -- opening, purchase, payment, purchase_return, adjustment
    direction VARCHAR(10) NOT NULL CHECK (direction IN ('debit', 'credit')),
    amount NUMERIC(14,2) NOT NULL CHECK (amount >= 0),
    reference_type VARCHAR(80),
    reference_id UUID,
    user_id UUID REFERENCES users(id),
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- ============================================================
-- 09. Stock movements
-- ============================================================

CREATE TABLE stock_movements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id),
    product_unit_id UUID REFERENCES product_units(id),
    quantity_base_unit NUMERIC(14,3) NOT NULL, -- موجب للداخل، سالب للخارج
    movement_type VARCHAR(80) NOT NULL, -- purchase, sale, sales_return, purchase_return, adjustment, damaged
    reference_type VARCHAR(80),
    reference_id UUID,
    user_id UUID REFERENCES users(id),
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_stock_movements_product ON stock_movements(product_id);
CREATE INDEX idx_stock_movements_date ON stock_movements(created_at);
CREATE INDEX idx_stock_movements_type ON stock_movements(movement_type);

-- View لحساب رصيد المخزون الحالي من الحركات
CREATE VIEW product_stock_balances AS
SELECT
    p.id AS product_id,
    p.name AS product_name,
    p.base_unit_id,
    COALESCE(SUM(sm.quantity_base_unit), 0) AS current_stock_base_qty
FROM products p
LEFT JOIN stock_movements sm ON sm.product_id = p.id
GROUP BY p.id, p.name, p.base_unit_id;

-- ============================================================
-- 10. Returns
-- ============================================================

CREATE TABLE returns (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    return_no VARCHAR(80) NOT NULL UNIQUE,
    return_type VARCHAR(50) NOT NULL CHECK (return_type IN ('sales_return', 'purchase_return')),
    customer_id UUID REFERENCES customers(id),
    supplier_id UUID REFERENCES suppliers(id),
    original_invoice_id UUID,
    total_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    refund_method VARCHAR(50), -- cash, customer_balance, supplier_balance
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    user_id UUID REFERENCES users(id),
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE return_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    return_id UUID NOT NULL REFERENCES returns(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id),
    product_unit_id UUID NOT NULL REFERENCES product_units(id),
    quantity NUMERIC(14,3) NOT NULL CHECK (quantity > 0),
    quantity_base_unit NUMERIC(14,3) NOT NULL CHECK (quantity_base_unit > 0),
    unit_price NUMERIC(14,2) NOT NULL,
    total_amount NUMERIC(14,2) NOT NULL,
    condition VARCHAR(50) NOT NULL DEFAULT 'good', -- good, damaged
    restock BOOLEAN NOT NULL DEFAULT TRUE,
    serial_id UUID REFERENCES product_serials(id),
    notes TEXT
);

-- ============================================================
-- 11. Alerts
-- ============================================================

CREATE TABLE alerts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    alert_type VARCHAR(80) NOT NULL, -- low_stock, credit_limit, overdue_invoice, backup_failed
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    severity VARCHAR(30) NOT NULL DEFAULT 'info', -- info, warning, critical
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    reference_type VARCHAR(80),
    reference_id UUID,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    read_at TIMESTAMP
);

CREATE INDEX idx_alerts_unread ON alerts(is_read);
CREATE INDEX idx_alerts_type ON alerts(alert_type);

-- ============================================================
-- 12. Backups and sync
-- ============================================================

CREATE TABLE backup_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    backup_type VARCHAR(50) NOT NULL, -- auto, manual, before_restore
    file_path TEXT NOT NULL,
    status VARCHAR(50) NOT NULL, -- success, failed
    message TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE sync_devices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_name VARCHAR(150) NOT NULL,
    device_type VARCHAR(50) NOT NULL, -- desktop, android
    device_token TEXT UNIQUE,
    last_sync_at TIMESTAMP,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE sync_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id UUID REFERENCES sync_devices(id),
    sync_direction VARCHAR(20) NOT NULL, -- upload, download
    entity_name VARCHAR(100) NOT NULL,
    entity_id UUID,
    status VARCHAR(50) NOT NULL, -- success, failed, conflict
    message TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- ============================================================
-- 13. Initial seed data
-- ============================================================

INSERT INTO units (name, symbol) VALUES
('قطعة', 'pcs'),
('كيلو', 'kg'),
('جرام', 'g'),
('طن', 'ton'),
('لتر', 'L'),
('متر', 'm'),
('شكارة', 'bag'),
('جردل', 'bucket'),
('علبة', 'box'),
('كرتونة', 'carton'),
('لفة', 'roll')
ON CONFLICT (name) DO NOTHING;

INSERT INTO price_groups (name, description, is_default) VALUES
('قطاعي', 'السعر العادي للعميل الفردي', TRUE),
('جملة', 'سعر الجملة', FALSE),
('مقاول', 'سعر خاص للمقاولين', FALSE),
('تاجر', 'سعر خاص للتجار', FALSE)
ON CONFLICT (name) DO NOTHING;

INSERT INTO expense_categories (name) VALUES
('إيجار'),
('كهرباء'),
('مياه'),
('نقل'),
('تحميل'),
('عمالة يومية'),
('صيانة'),
('إنترنت'),
('مصروفات أخرى')
ON CONFLICT (name) DO NOTHING;

INSERT INTO roles (name, description) VALUES
('Owner', 'صاحب المحل - كل الصلاحيات'),
('Cashier', 'كاشير - بيع واستعلام وطباعة'),
('StoreKeeper', 'مسؤول مخزن - منتجات وجرد'),
('MobileUser', 'مستخدم موبايل')
ON CONFLICT (name) DO NOTHING;

INSERT INTO cashboxes (name, opening_balance, current_balance) VALUES
('الخزينة الرئيسية', 0, 0)
ON CONFLICT (name) DO NOTHING;

-- ============================================================
-- End of Phase 01 schema
-- ============================================================
