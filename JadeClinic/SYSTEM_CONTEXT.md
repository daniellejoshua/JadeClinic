# JadeClinic — System Context

> **Purpose:** Self-contained reference for other AI agents (and human contributors) working on this codebase. Read this before touching code.
>
> **Currency note:** The system is mid-migration from a shared SQLite file to **PostgreSQL**. This document describes the **target architecture (PostgreSQL-first)** and flags the in-flight migration status in [§2.5](#25-migration-status-sqlite-to-postgresql). Version numbers below refer to release builds shipped to the LAN.

---

## 1. Overview & Goals

JadeClinic is a **retail/wholesale point-of-sale + back-office system** for a Philippine dental/supply business. It runs entirely on-premises on a small LAN with multiple PC terminals sharing one database.

### Primary goals
- **One source of truth.** All terminals (POS + admin) read/write a single central database; no per-PC copies to reconcile.
- **Fast, cashier-friendly POS.** Product-card grid, barcode scanning, cart with discounts, payment capture, thermal receipt printing.
- **Full back-office.** Inventory, stock movements, sales history, staff & ID cards, suppliers, audit trail, company settings.
- **Multi-terminal safety.** Cross-PC stock, sales and approval state bootstrapped from the central DB; new terminals must be approved before they can operate.
- **Cheap, resilient updates.** Automatic in‑app updates pulled from a network share, no per-PC manual installs.
- **Cloud mirror for reporting + web admin.** A copy of business data is pushed to Supabase (Postgres) in the cloud. This cloud copy is the data source for a future admin-only **read-only web dashboard with an AI agent** (§10).

### Non-goals (by design)
- No near/hard realtime cross-terminal cart reservation. Carts live only in the local terminal's memory; correctness is guaranteed at checkout via an atomic conditional stock decrement (§6.4).
- No offline-first architecture; the app requires the LAN to reach the database.

---

## 2. Multi-PC Architecture (LAN)

### 2.1 Topology
- **Admin PC** — hostname `DANIELLE`, IP `192.168.0.100`. The authoritative machine:
  - hosts the **database server** and the **updates share**;
  - approves/refuses new terminals;
  - performs system-level actions.
- **Staff / POS terminals** — `…192.168.0.101` and up. Run the POS and read the central DB over the LAN.
- **Router** — LAN connectivity between PCs (`192.168.0.1`). Both PCs must be on the network for the app to work.

### 2.2 Database access model
- **Target (this doc):** one **PostgreSQL** instance on the admin PC (port **5432**), dedicated database + user. Every terminal connects over TCP via **Npgsql**. Same dialect as the cloud (Supabase) — see §9.
- **Legacy (current, pre-1.1.6.0):** a single shared SQLite file `jadeclinic.db` located on the LAN share `\\192.168.0.100\JadeClinicData\`, opened directly by all terminals over SMB. This worked but is the known weak link (SMB locking, corruption risk) — the reason for the migration. Connection details are centralised in `Connection.vb` (`GetConnectionString`, `GetDefaultDatabasePath`).

### 2.3 Terminal registration & control
- A terminal registers by hostname (see `Utilities\TerminalManager.vb`, `Utilities\TerminalMigration.vb`, `Forms\System\TerminalSetup.vb`).
- New terminals appear as **pending** and must be **approved** on the admin PC (`Forms\System\TerminalSetup.vb`, `Forms\System\TerminalApprovalWaitForm.vb` blocks until approved).
- Terminals can be **blocked**; approvals/blocking are keyed to the admin **hostname** so the gate cannot be defeated from a staff machine.
- Relevant modules: `Utilities\TerminalManager.vb`, `Modules\IdleTimeoutManager.vb` (auto-lock), `Forms\System\SyncLogs.vb` (PC online status + sync health).

### 2.4 Multi-PC heuristics (simplified)
- Reads: any PC may read the central DB at any time.
- Writes: serialised by the DB itself (atomic statements + transactions).
- Cross-PC stock visibility: achieved by lightweight polling of a **SyncMarker** (§6), not by push/websockets.

### 2.5 Migration status — SQLite to PostgreSQL
- **Current shipped** (1.1.5.0): **SQLite over SMB**. Upgrade path per `Utilities\SyncMarkerMigration.vb`, `SalesStatusMigration.vb`, `EmployeeCodeMigration.vb`, `BatchTrackingMigration.vb`, `TerminalMigration.vb`.
- **Next release (1.1.6.0): migrate to PostgreSQL.** Plan:
  - native PostgreSQL installer on the admin PC (no Docker), port 5432;
  - create dedicated database + user; open firewall for the LAN;
  - migrate schema + data from SQLite (18+ files touch `Microsoft.Data.Sqlite`, ~191 `Sqlite*` type usages to convert; SQLite-only syntax to rewrite: `datetime('now')`, `strftime`, `INSERT OR IGNORE`, `AUTOINCREMENT`, `PRAGMA`, `VACUUM`, `sqlite_sequence`);
  - switch `Connection.vb` to Npgsql connection strings;
  - backups via `pg_dump`.
- Until completed, `Microsoft.Data.Sqlite` is the live driver and **Npgsql** is wired for the cloud sync path.

---

## 3. Tech Stack

| Concern | Choice |
|---|---|
| Language / UI | **VB.NET, WinForms** (Windows only) |
| Framework | `.NET 8`, `net8.0-windows` |
| Deployment | Self-contained **single-file** exe, `win-x64` |
| UI kit | Guna.UI2 |
| Charts | LiveChartsCore + SkiaSharp |
| PDF / print layouts | QuestPDF |
| Barcode / QR | MessagingToolkit QR (login QR, staff ID QR) |
| Images | `Utilities\ImageCompression.vb` (downscale on upload) |
| Database — target | **PostgreSQL via Npgsql** (`BIGSERIAL` identities, PG types) |
| Database — legacy | `Microsoft.Data.Sqlite` (being phased out) |
| Cloud DB | Supabase (managed **Postgres**) |
| JSON | Newtonsoft.Json |
| Passwords | BCrypt hashing |
| Syntax / IDE | Visual Studio, solution `JadeClinic.sln` |

Sensitive config (`UpdatePath`, `DBPath`, `SupabaseConnectionString`) lives in `app.config` → `JadeClinic.dll.config`.

---

## 4. Roles & Security

### 4.1 Roles
- **STAFF** — POS-only permissions.
- **MANAGER** — POS + back-office (staff, inventory adjustments, supplier, records).
- **ADMIN** — everything, including terminals, system settings, approvals.

Navigation is built per-role (`Shell\MainShell.vb` `EnforceRoleAccess`, `Shared\NavigationBuilder.vb`, `Shared\HeaderUserControl.vb`) — approved pages only; the enforcement is in the shell, not just hidden buttons.

### 4.2 Login flow (`Forms\Login\frmLoginvb.vb`)
1. **Username + password** (BCrypt) → then a **4-digit PIN**.
2. **QR login**: admins/managers can log a terminal in by scanning a QR.
3. **PIN attempt lockout**: exceeding `MaxPinAttempts` writes an audit entry and closes the app.
4. **Forgot PIN**: `Forms\Login\ForgotPasswordForm.vb` resets via **3 registered passkeys** (`Utilities\PasskeyManager.vb`) — recorded to audit log.
5. **Idle timeout**: `Modules\IdleTimeoutManager.vb` auto-locks after inactivity.

### 4.3 Default accounts (change in production)
- `admin` / `admin` — PIN `1234`
- `manager` / `manager123` — PIN `5678`
- `staff1`–`staff3` / `staff123` — PINs `1111/2222/3333`

### 4.4 Audit trail
Every sensitive action (login, PIN reset, void, capital unlock, terminal approval, config change) is written to `AuditLog` via `Utilities` logging helpers and viewable/exportable in `Forms\AuditLog\AuditLog.vb` (`AuditExporter.vb`).

---

## 5. Feature Catalogue

Modules below map 1:1 to folders under `Forms\`.

### 5.1 Login & Shell
- `Forms\Login\` — password/PIN login, QR login, forgot-PIN (passkeys), version label, **pre-login auto-update check** (§8).
- `Forms\Shell\MainShell.vb` — app frame: role-based nav, header, page switching.
- `Forms\components\EscForm.vb`, `Forms\Shared\` — base/dialog helpers (`EscPosPreviewForm.vb`, `ProfileManager.vb`, `DataGridViewHelper.vb`).

### 5.2 Dashboard (`Dashboard.vb`)
- KPI cards: orders, revenue, gross profit, stock value.
- Charts: sales trend, inventory status (pie), top products, margin strip.
- Near-realtime stock KPIs via SyncMarker polling (5s), §6.

### 5.3 Sales / POS (`Forms\Sales\Sales.vb`)
- Product-card grid with pagination (`PaginationControl.vb`), categories, search, barcode.
- Cart (`OrderSummaryGridBuilder.vb`, `EmptyCartStateBuilder.vb`), quantities, customer info, discounts.
- Payment methods: **Cash, Card, E-Wallet (GCash / Maya)**, guaranteed/tendered amounts, change.
- **VAT 12%** (Philippine), peso formatting (`en-PH`).
- **Sale ID tagging**: sale IDs are prefixed per terminal (e.g. `PC1-…`) so records are traceable to a machine.
- **Capital Lock**: the day's opening capital must be unlocked (admin/manager) before first sale; audit-logged.
- **Stock guard (1.1.5.0)**: `ValidateCartStockBeforePayment()` reads fresh stock before payment — if quantity changed / insufficient, blocks with "Stock Changed … only N left" and refreshes labels.
- **Checkout atomicity**: stock decrements via `UPDATE Products SET CurrentStock = CurrentStock - @Qty WHERE ProductID = @ProductID AND CurrentStock >= @Qty` — last-writer-wins, prevents oversell.
- Void/approval flows for corrections (approved by role), receipt reprint.
- Cart draft persisted between sessions.
- Receipts: `Modules\ReceiptRenderer.vb`, `Modules\EscPosPrinter.vb` (ESC/POS thermal; live/PDF preview).
- Sales reprint/status: `Utilities\SalesStatusMigration.vb`.

### 5.4 Inventory (`Forms\Inventory\`)
- Product CRUD (`AddProduct.vb`): codes, category, unit, barcode; **cost / selling / wholesale** prices; reorder level; active/inactive; images (`ProductImages` + mapping, compressed).
- Batch tracking (`Utilities\BatchTrackingMigration.vb`, `InventoryLog`).
- Stock value, low-stock / out-of-stock coloring, search/filter, export (`InventoryExporter.vb`), 2s realtime polling.

### 5.5 Inventory Log (`Forms\InventoryLog\`)
- Stock in / out / adjustments / restock entries (`AddInventoryLogForm.vb`), export, 2s realtime polling; feeds `InventoryLog` table.

### 5.6 Sales Record (`Forms\Sales_Record\`)
- Historical sales list + detail view (`SalesDetails.vb`), reprint, export (`SalesRecordExporter.vb`).
- `SalesStatusMigration.vb` adds a status column for lifecycle tracking.

### 5.7 Staff (`Forms\Staff\`)
- Employee records (`Staff.vb`, `AddStaff.vb`), roles, **ID cards with QR** (`IdCard.vb` / `IdCardBack.vb`), export (`StaffExporter.vb`).
- `Utilities\EmployeeCodeMigration.vb` adds employee codes.

### 5.8 Users (`Forms\UserManagement\AddUserForm.vb`)
- Login accounts for the system, linked to roles.

### 5.9 Suppliers (`Forms\Supplier\`)
- Supplier directory, CRUD, export (`SupplierExporter.vb`); `Suppliers` table.

### 5.10 Audit Log (`Forms\AuditLog\`)
- Security/significant events, filter, export (`AuditExporter.vb`).

### 5.11 System
- `Forms\System\CompanySettings.vb` (+ `Modules\CompanySettingsManager.vb`) — business name, TIN, address, logo, print header.
- `Forms\System\Sys.vb` — system/config page.
- `Forms\System\SyncLogs.vb` — cloud sync status, PC presence.
- `Forms\System\TerminalSetup.vb` — terminal register/approve/block.
- Update UI: `UpdateProgressForm.vb`, `ApplyingUpdateForm.vb` (§8).

### 5.12 Profile (`Forms\ProfileSettings\`)
- Change password/PIN, manage passkeys, profile details.

### 5.13 First-run
- `Forms\FormFirstRun.vb` — first-launch setup (company + admin).

---

## 6. Realtime Between PCs

### 6.1 Problem
Multiple terminals share one DB; a terminal must notice stock changes made by another terminal *and* stay correct when two terminals hit the same product at once.

### 6.2 SyncMarker
`SyncMarker` is a single-row table (`MarkerID = 1`, `LastChange`) bumped by DB **triggers** whenever `Products.CurrentStock` changes via update/insert/delete (`Utilities\SyncMarkerMigration.vb`).
- **1.1.5.0 hardening:** body rewritten from `strftime(...)` timestamps to a **monotonic integer counter**: `LastChange = CAST(CAST(LastChange AS INTEGER) + 1 AS TEXT)`. Every change yields a unique, monotonic value regardless of timing. Migration **drops and recreates** the triggers (`DROP TRIGGER IF EXISTS …` then `CREATE TRIGGER`) so existing DBs pick up the new body.
- Trigger-based change detection keeps clients read-only-cheap: they never write to the marker themselves.

### 6.3 Polling
`Utilities\SyncMarkerTracker.vb` caches the last-seen marker value; timers poll and refresh on change:
- Sales POS + Inventory + Inventory Log: **~2s**
- Dashboard stock KPIs/charts: **~5s**

### 6.4 Concurrency safety
- Carts are **local in-memory only** (no shared reservation).
- The checkout decrement is a single atomic conditional `UPDATE … WHERE CurrentStock >= @Qty` inside a transaction → two terminals cannot oversell the last unit (§5.3).
- If stock changed since the cart was built, `ValidateCartStockBeforePayment()` refuses payment and shows the current shortfall *before* money changes hands.

---

## 7. Tables & Data

Base schema created by `DatabaseInitializer.vb`; safe migrations add columns/tables (see §2.5).

| Table | Purpose |
|---|---|
| `Users` | Login accounts, BCrypt hash, role, PIN, status |
| `Suppliers` | Supplier directory |
| `Products` | SKU/code, name, category, unit, barcode, cost/sell/wholesale, reorder, **CurrentStock** |
| `ProductImages` | Blob/image storage |
| `ProductImageMapping` | Product ↔ image link |
| `Sales` | Sale header: terminal-tagged ID (`PC1-…`), date, cashier, subtotal, VAT, total, payment detail |
| `SaleItems` | Line items, prices, quantities |
| `InventoryLog` | Stock in/out/adjust/restock movements (+ batch fields via `BatchTrackingMigration.vb`) |
| `AuditLog` | Security & significant events |
| `CompanySettings` | Business identity, logo, print/header settings |

Migration-created additions: `SyncMarker`, `Terminals` (registration state), sale `status`, employee codes, batch-tracking columns.

On PostgreSQL: `BIGSERIAL` identities, proper `TIMESTAMPTZ`, rely on PG integrity over SMB's.

---

## 8. Auto-Update

### 8.1 Mechanics
- Update source: LAN share `\\192.168.0.100\JadeClinicUpdates` (folder `C:\JadeClinicUpdates` on the admin PC).
- **Version file** `version.txt` holds the newest version string; client compares and updates iff newer.
- Bundle: `JadeClinic.exe` (self-contained single-file), `JadeClinic.dll.config`, `LatoFont\` fonts, `version.txt`. No nested `publish\` subfolder.
- **Pre-login check** (`Forms\Login\frmLoginvb.vb`): silent version check before login, always auto-update (no prompt), version label shown on login and every page.

### 8.2 Apply flow (`Utilities\AutoUpdater.vb`)
1. Download new bundle to `%LOCALAPPDATA%\JadeClinic\Update\`.
2. Show update progress (`UpdateProgressForm.vb`), then relaunch the **staged** exe with `--apply-update "<updateDir>" "<appDir>" <pid>` and exit.
3. The updater instance shows a blocking marquee window — `Forms\System\ApplyingUpdateForm.vb` — waits for the old process to exit (`WaitForProcessExit`), copies `exe/config/LatoFont` with retry (`CopyFileWithRetry`, `CopyDirectoryWithRetry`), then starts the new build.
4. Staging folder may not fully delete (running exe locked); the next update cleans any leftovers. `ApplicationEvents.vb` routes the `--apply-update` run into the updater path (skips DB init/terminal registration).

### 8.3 Seeding a release (manual, done on admin PC)
1. `dotnet publish … -c Release -r win-x64 --self-contained true`
2. Verify file version == target, then copy `JadeClinic.exe`, `JadeClinic.dll.config`, `LatoFont\` to the share and set `version.txt`.

---

## 9. Cloud Sync, Backups & Supabase

- **Supabase** provides the managed Postgres cloud database (the same engine the local system is migrating to).
- Sync path (`.NET, no separate service`): `Modules\SupabaseSync.vb` + `Modules\SyncQueue.vb` — a **debounced, scheduled** push of business data into Supabase Postgres; `Modules\LocalSyncLog.vb` records each sync run, surfaced in `Forms\System\SyncLogs.vb`.
- Npgsql is the client already used for the cloud, doubling as the future local driver — one dialect end to end.
- `Modules\DatabaseSnapshot.vb` snapshots the local DB (`VACUUM INTO …` today → `pg_dump` after migration) for on-box backups.
- `Modules\DemoDataSeeder.vb` seeds demo data for development/trial.

---

## 10. Planned: Web Admin (Read-only + AI)

A **browser-based admin dashboard**, currently planned (not yet built):

- **Audience:** administrators only.
- **Access mode:** **read-only** — it reads data and never writes to the LAN database.
- **Data source:** **Supabase (cloud Postgres)** — consumes the already-synced cloud copy, so admins can reach it from outside the LAN (internet).
- **AI agent** — scope is evolving; current ambitions:
  - **Natural-language Q&A** over sales/inventory (e.g. "what sold most this week?", "net profit in March?") with answers/charts;
  - **Insights & alerts** — low stock, slow movers, daily digests, anomalies;
  - **Report generation** — on-demand sales/inventory/audit reports (PDF/CSV).
- Architectural consequence: the web stack speaks Postgres/Supabase directly and needs no connection to the clinic network.

---

## 11. Roadmap & Known Limitations

**In flight**
- **PostgreSQL migration (1.1.6.0)** — replace shared SQLite-over-SMB with local Postgres on the admin PC; the main correctness/robustness driver given the cloud is already Postgres (§2.5).
- Web admin + AI agent (§10).

**Known limitations / notes**
- SMB-shared SQLite is the current weak link (locking, corruption risk) → motivating the migration.
- No cross-terminal cart reservation — enforced atomic checkout instead (§6.4).
- Self-contained single-file exe is large (~220 MB); accepted for a LAN product.
- Low-priority UI debt: grid pagination alignment offsets inconsistent across Staff/AuditLog/InventoryLog/SalesRecord.
- Default demo accounts must be changed in production (§4.3).

---

## Appendix — File Map (key locations)

```
JadeClinic/
  Connection.vb                     DB connections (SQLite today → Npgsql)
  DatabaseInitializer.vb            Base schema
  ApplicationEvents.vb              Startup, --apply-update routing
  app.config                        UpdatePath, DBPath, SupabaseConnectionString
  Forms/
    Login/   Shell/   Dashboard.vb  Sales/   Inventory/   InventoryLog/
    Sales_Record/   Staff/   UserManagement/   Supplier/   AuditLog/
    System/   ProfileSettings/   components/   Shared/
  Modules/                          SyncQueue, SupabaseSync, ReceiptRenderer,
                                    EscPosPrinter, DatabaseSnapshot, LocalSyncLog,
                                    IdleTimeoutManager, DemoDataSeeder,
                                    CompanySettingsManager
  Utilities/                        AutoUpdater, TerminalManager, SyncMarkerTracker,
                                    SyncMarkerMigration, PasskeyManager,
                                    ImageCompression, …Migrations, …
```