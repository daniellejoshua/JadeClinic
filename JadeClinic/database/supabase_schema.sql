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
-- COMPANY SETTINGS
-- ============================================================
CREATE TABLE company_settings (
    id                SERIAL PRIMARY KEY,
    local_id          INTEGER UNIQUE NOT NULL,
    company_name      TEXT NOT NULL,
    tin               TEXT,
    address           TEXT,
    phone             TEXT,
    email             TEXT,
    website           TEXT,
    logo_url          TEXT,
    bir_auth_number   TEXT,
    ptu_number        TEXT,
    validity_years    INTEGER DEFAULT 5,
    receipt_footer    TEXT,
    company_hours     TEXT,
    is_active         BOOLEAN DEFAULT TRUE,
    created_at        TIMESTAMPTZ DEFAULT NOW(),
    updated_at        TIMESTAMPTZ DEFAULT NOW(),
    synced_at         TIMESTAMPTZ
);

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
ALTER TABLE inventory_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE company_settings ENABLE ROW LEVEL SECURITY;

-- Authenticated users can read, only service role can write
CREATE POLICY "read_only" ON users FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON suppliers FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON products FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON sales FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON sale_items FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON inventory_logs FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON audit_logs FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "read_only" ON company_settings FOR SELECT USING (auth.role() = 'authenticated');
