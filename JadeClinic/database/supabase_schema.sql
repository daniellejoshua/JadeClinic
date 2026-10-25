-- Supabase Schema for JadeClinic Admin Dashboard
-- Read-only dashboard. Data is synced from LAN SQLite.
-- No BLOBs — images stored as files on LAN, synced to Supabase Storage.

-- ============================================================
-- USERS
-- ============================================================
CREATE TABLE users (
    id            SERIAL PRIMARY KEY,
    local_id      INTEGER UNIQUE NOT NULL,
    username      TEXT NOT NULL,
    full_name     TEXT NOT NULL,
    user_role     TEXT DEFAULT 'Staff',
    is_active     BOOLEAN DEFAULT TRUE,
    email         TEXT,
    phone         TEXT,
    photo_url     TEXT,
    employee_code TEXT,
    created_at    TIMESTAMPTZ DEFAULT NOW(),
    updated_at    TIMESTAMPTZ DEFAULT NOW(),
    synced_at     TIMESTAMPTZ
);

CREATE INDEX idx_users_role ON users(user_role);

-- ============================================================
-- SUPPLIERS
-- ============================================================
CREATE TABLE suppliers (
    id              SERIAL PRIMARY KEY,
    local_id        INTEGER UNIQUE NOT NULL,
    supplier_code   TEXT NOT NULL,
    supplier_name   TEXT NOT NULL,
    contact_person  TEXT,
    phone           TEXT,
    email           TEXT,
    is_active       BOOLEAN DEFAULT TRUE,
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    updated_at      TIMESTAMPTZ DEFAULT NOW(),
    synced_at       TIMESTAMPTZ
);

-- ============================================================
-- PRODUCTS
-- ============================================================
CREATE TABLE products (
    id              SERIAL PRIMARY KEY,
    local_id        INTEGER UNIQUE NOT NULL,
    product_code    TEXT NOT NULL,
    product_name    TEXT NOT NULL,
    category        TEXT,
    unit            TEXT DEFAULT 'PCS',
    current_stock   INTEGER DEFAULT 0,
    reorder_level   INTEGER DEFAULT 10,
    has_expiry      BOOLEAN DEFAULT FALSE,
    expiry_date     TEXT,
    cost_price      DECIMAL(10,2) NOT NULL,
    selling_price   DECIMAL(10,2) NOT NULL,
    wholesale_price DECIMAL(10,2),
    supplier_id     INTEGER REFERENCES suppliers(id),
    image_url       TEXT,
    is_active       BOOLEAN DEFAULT TRUE,
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    updated_at      TIMESTAMPTZ DEFAULT NOW(),
    synced_at       TIMESTAMPTZ
);

CREATE INDEX idx_products_category ON products(category);
CREATE INDEX idx_products_supplier ON products(supplier_id);
CREATE INDEX idx_products_stock_alert ON products(current_stock) WHERE current_stock <= reorder_level;

-- ============================================================
-- SALES
-- ============================================================
CREATE TABLE sales (
    id              SERIAL PRIMARY KEY,
    local_id        INTEGER UNIQUE NOT NULL,
    sale_number     TEXT,
    sale_date       TIMESTAMPTZ DEFAULT NOW(),
    customer_name   TEXT,
    customer_tin    TEXT,
    user_id         INTEGER REFERENCES users(id),
    total_amount    DECIMAL(10,2) DEFAULT 0,
    amount_paid     DECIMAL(10,2) DEFAULT 0,
    payment_method  TEXT DEFAULT 'Cash',
    reference       TEXT,
    status          TEXT DEFAULT 'Completed',
    approved_by     TEXT,
    abort_reason    TEXT,
    discount_type   TEXT,
    discount_amount DECIMAL(10,2) DEFAULT 0,
    sales_data      JSONB,
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    synced_at       TIMESTAMPTZ
);

CREATE INDEX idx_sales_date ON sales(sale_date);
CREATE INDEX idx_sales_user ON sales(user_id);
CREATE INDEX idx_sales_status ON sales(status);

-- ============================================================
-- SALE ITEMS
-- ============================================================
CREATE TABLE sale_items (
    id                  SERIAL PRIMARY KEY,
    local_id            INTEGER UNIQUE NOT NULL,
    sale_id             INTEGER REFERENCES sales(id) ON DELETE CASCADE,
    product_id          INTEGER REFERENCES products(id),
    quantity            INTEGER NOT NULL,
    unit_price          DECIMAL(10,2) NOT NULL,
    original_unit_price DECIMAL(10,2),
    line_discount       DECIMAL(10,2) DEFAULT 0,
    sub_total           DECIMAL(10,2),
    created_at          TIMESTAMPTZ DEFAULT NOW(),
    synced_at           TIMESTAMPTZ
);

CREATE INDEX idx_sale_items_sale ON sale_items(sale_id);
CREATE INDEX idx_sale_items_product ON sale_items(product_id);

-- ============================================================
-- SALES RETURNS (customer returns / exchanges)
-- ============================================================
CREATE TABLE sales_returns (
    id              SERIAL PRIMARY KEY,
    local_id        INTEGER UNIQUE NOT NULL,
    sale_id         INTEGER REFERENCES sales(id),
    return_number   TEXT,
    return_date     TIMESTAMPTZ DEFAULT NOW(),
    return_type     TEXT,
    reason          TEXT,
    refund_amount   DECIMAL(10,2) DEFAULT 0,
    additional_payment_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    settlement_method TEXT,
    settlement_reference TEXT,
    approved_by     TEXT,
    approved_by_user_id INTEGER REFERENCES users(id),
    processed_by    INTEGER REFERENCES users(id),
    initiated_by    INTEGER REFERENCES users(id),
    notes           TEXT,
    status          TEXT DEFAULT 'Completed',
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    synced_at       TIMESTAMPTZ,
    CONSTRAINT ck_sales_returns_type CHECK (return_type IN ('Refund','Exchange','Mixed')),
    CONSTRAINT ck_sales_returns_status CHECK (status IN ('Completed','Cancelled','Reversed')),
    CONSTRAINT ck_sales_returns_amounts CHECK (refund_amount >= 0 AND additional_payment_amount >= 0)
);

CREATE INDEX idx_sales_returns_sale ON sales_returns(sale_id);
CREATE INDEX idx_sales_returns_date ON sales_returns(return_date);
CREATE UNIQUE INDEX ux_sales_returns_number ON sales_returns(return_number) WHERE return_number IS NOT NULL;
CREATE INDEX idx_sales_returns_approved_by_user ON sales_returns(approved_by_user_id);
CREATE INDEX idx_sales_returns_processed_by ON sales_returns(processed_by);
CREATE INDEX idx_sales_returns_initiated_by ON sales_returns(initiated_by);

-- ============================================================
-- SALES RETURN ITEMS
-- ============================================================
CREATE TABLE sales_return_items (
    id                      SERIAL PRIMARY KEY,
    local_id                INTEGER UNIQUE NOT NULL,
    return_id               INTEGER REFERENCES sales_returns(id),
    original_sale_item_id   INTEGER REFERENCES sale_items(id),
    product_id              INTEGER REFERENCES products(id),
    quantity                INTEGER NOT NULL,
    unit_price              DECIMAL(10,2) DEFAULT 0,
    condition               TEXT,
    disposition             TEXT,
    line_refund_amount      DECIMAL(10,2) DEFAULT 0,
    return_credit_amount    DECIMAL(18,2) NOT NULL DEFAULT 0,
    replacement_product_id  INTEGER REFERENCES products(id),
    replacement_quantity    INTEGER,
    replacement_unit_price  DECIMAL(10,2),
    created_at              TIMESTAMPTZ DEFAULT NOW(),
    synced_at               TIMESTAMPTZ,
    CONSTRAINT ck_sales_return_items_quantity CHECK (quantity > 0),
    CONSTRAINT ck_sales_return_items_condition CHECK (condition IN ('Good','Damaged')),
    CONSTRAINT ck_sales_return_items_disposition CHECK (disposition IN ('Quarantine','Restock','WriteOff','ReturnToVendor')),
    CONSTRAINT ck_sales_return_items_amounts CHECK (line_refund_amount >= 0 AND return_credit_amount >= line_refund_amount),
    CONSTRAINT ck_sales_return_items_damaged CHECK (condition <> 'Damaged' OR line_refund_amount = 0),
    CONSTRAINT ck_sales_return_items_replacement CHECK (
        (replacement_product_id IS NULL AND replacement_quantity IS NULL) OR
        (replacement_product_id IS NOT NULL AND replacement_quantity > 0 AND replacement_unit_price >= 0)
    )
);

CREATE INDEX idx_sales_return_items_return ON sales_return_items(return_id);
CREATE INDEX idx_sales_return_items_product ON sales_return_items(product_id);
CREATE INDEX idx_sales_return_items_original ON sales_return_items(original_sale_item_id);
CREATE INDEX idx_sales_return_items_replacement ON sales_return_items(replacement_product_id);

-- ============================================================
-- RETURN QUARANTINE
-- ============================================================
CREATE TABLE return_quarantine (
    id              SERIAL PRIMARY KEY,
    local_id        INTEGER UNIQUE NOT NULL,
    return_item_id  INTEGER UNIQUE NOT NULL REFERENCES sales_return_items(id),
    product_id      INTEGER NOT NULL REFERENCES products(id),
    quantity        INTEGER NOT NULL,
    item_condition  TEXT NOT NULL,
    status          TEXT NOT NULL DEFAULT 'Pending',
    received_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resolved_at     TIMESTAMPTZ,
    resolved_by     INTEGER REFERENCES users(id),
    notes           TEXT,
    synced_at       TIMESTAMPTZ,
    CONSTRAINT ck_return_quarantine_quantity CHECK (quantity > 0),
    CONSTRAINT ck_return_quarantine_status CHECK (status IN ('Pending','Restocked','WrittenOff','ReturnedToVendor'))
);

CREATE INDEX idx_return_quarantine_status ON return_quarantine(status, product_id);
CREATE INDEX idx_return_quarantine_resolved_by ON return_quarantine(resolved_by);

-- ============================================================
-- INVENTORY LOG
-- ============================================================
CREATE TABLE inventory_logs (
    id              SERIAL PRIMARY KEY,
    local_id        INTEGER UNIQUE NOT NULL,
    product_id      INTEGER REFERENCES products(id),
    transaction_type TEXT NOT NULL CHECK (transaction_type IN ('IN', 'OUT', 'ADJUST')),
    quantity        INTEGER NOT NULL,
    previous_stock  INTEGER,
    new_stock       INTEGER,
    batch_number    TEXT,
    expiry_date     TEXT,
    supplier_id     INTEGER REFERENCES suppliers(id),
    user_id         INTEGER REFERENCES users(id),
    reference       TEXT,
    notes           TEXT,
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    synced_at       TIMESTAMPTZ
);

CREATE INDEX idx_inventory_log_product ON inventory_logs(product_id);
CREATE INDEX idx_inventory_log_date ON inventory_logs(created_at);
CREATE INDEX idx_inventory_log_type ON inventory_logs(transaction_type);

-- ============================================================
-- AUDIT LOG
-- ============================================================
CREATE TABLE audit_logs (
    id          SERIAL PRIMARY KEY,
    local_id    INTEGER UNIQUE NOT NULL,
    action      TEXT NOT NULL,
    details     TEXT,
    user_id     INTEGER REFERENCES users(id),
    action_time TIMESTAMPTZ DEFAULT NOW(),
    synced_at   TIMESTAMPTZ
);

CREATE INDEX idx_audit_log_user ON audit_logs(user_id);
CREATE INDEX idx_audit_log_time ON audit_logs(action_time);

-- ============================================================
-- SYNC LOG (tracks sync runs)
-- ============================================================
CREATE TABLE sync_log (
    id          SERIAL PRIMARY KEY,
    started_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMPTZ,
    rows_synced INTEGER DEFAULT 0,
    status      TEXT DEFAULT 'running',
    error       TEXT
);

-- ============================================================
-- ROW LEVEL SECURITY (Supabase specific)
-- All tables are read-only for dashboard users.
-- ============================================================
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE suppliers ENABLE ROW LEVEL SECURITY;
ALTER TABLE products ENABLE ROW LEVEL SECURITY;
ALTER TABLE sales ENABLE ROW LEVEL SECURITY;
ALTER TABLE sale_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE sales_returns ENABLE ROW LEVEL SECURITY;
ALTER TABLE sales_return_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE return_quarantine ENABLE ROW LEVEL SECURITY;
ALTER TABLE inventory_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;

-- Authenticated users can read, only service role can write
CREATE POLICY "read_only" ON users FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON suppliers FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON products FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON sales FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON sale_items FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON sales_returns FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON sales_return_items FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON return_quarantine FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON inventory_logs FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON audit_logs FOR SELECT USING (auth.role() = 'authenticated');
