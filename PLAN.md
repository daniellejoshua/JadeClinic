# JadeClinic — Remote Dashboard & Sync Plan

## 1. Goal
Give the clinic owner a **remote read-only dashboard** of POS data from anywhere, plus a **central-DB setup** where staff terminals share one SQLite DB hosted on the admin PC. Cloud stack: Supabase (Postgres + Storage) + Laravel dashboard + Python AI insights. A `JadeSync` worker pushes LAN data → Supabase.

## 2. Architecture

```
CLINIC LAN                                 ADMIN PC                              CLOUD
┌──────────┐                    ┌───────────────────────────────────┐    ┌───────────────┐
│ Staff POS│                    │  Central SQLite (jadeclinic.db)   │    │  Supabase     │
│ Staff POS├── connect to ─────▶│  ◀— admin POS connects here too   │    │  · Postgres   │
│ Staff POS│   admin's SQLite   │                                   │    │  · Storage    │
└──────────┘  (no local DB)     │  JadeSync worker                  ├───▶│  (images)     │
                                │   · scheduled poll                │    └───────▲───────┘
                                │   · sync button (System Settings) │            │
                                │   · hooks: sale ✓ / stock ✓       │    ┌───────┴───────┐
                                └───────────────────────────────────┘    │ Laravel dash  │
                                                                         │ + FastAPI AI  │
                                                                         │ (read-only,   │
                                                                         │  separate repo)│
                                                                         └───────────────┘
```

- **One authoritative DB** → `local_id` is globally unique → Supabase schema works as-is.

## 3. Central DB (POS change)
- Replace per-machine `%LocalAppData%\JadeClinic\jadeclinic.db` with a **configurable DB path** (`app.config` → `DatabaseLocation`/`JadeDentalConnection`).
- Admin PC shares the folder; staff config points to `\\ADMIN-PC\JadeClinic\jadeclinic.db`.
- Staff terminals have **no local DB** — they write straight to the central file.
- SQLite already created with `PRAGMA journal_mode=WAL` + `busy_timeout=5000` (`database/sqlite_migration.sql`). Keep `Cache=Shared;Mode=ReadWriteCreate`.
- **Risk (Phase 1 spike):** verify shared-folder SQLite on the real LAN before building everything on it.

## 4. Supabase side (schema finalized)
- Schema: `JadeClinic/database/supabase_schema.sql` — 9 tables, `local_id` UNIQUE mapping, `sales_data JSONB`, `has_expiry`/`expiry_date` on products, image URL columns, RLS read-only, `sync_log`.
- Apply once via **Supabase SQL Editor** (not via Laravel migrations — the web app's role can't create tables).
- Storage: **one public bucket `Jade_Web_Bucket`** with an `images/` prefix containing `avatar/`, `product/`, `company/` (all-lowercase). Public URL: `https://<ref>.supabase.co/storage/v1/object/public/Jade_Web_Bucket/images/<subfolder>/<file>`.
- Roles:
  - Sync worker → **write** (Postgres connection with write perms; later S3 keys for images)
  - Web app + AI → **`dashboard_ro`** (SELECT-only)
    ```sql
    CREATE ROLE dashboard_ro LOGIN PASSWORD '<strong-password>';
    GRANT USAGE ON SCHEMA public TO dashboard_ro;
    GRANT SELECT ON ALL TABLES IN SCHEMA public TO dashboard_ro;
    ```

## 5. JadeSync (LAN → Supabase)
- .NET 8 logic in the POS repo. New `Modules/SupabaseSync.vb` (already built: full 8-table sync + test connection).
- **Triggers**
  | Trigger | How |
  |---|---|
  | **Sync button** | System Settings page → `SupabaseSync.RunFullSync()` on background task |
  | **Worker/poll** | Task Scheduler every N minutes |
  | **Instant hooks** | after (a) sale finalizes, (b) product stock / inventory log change |
- **Algorithm (per run)**
  1. Read all rows from central SQLite (read-only).
  2. **Full upsert** by `local_id`: `INSERT ... ON CONFLICT (local_id) DO UPDATE`.
  3. FK resolution order (Supabase FKs point at serial `id`s, not `local_id`s):
     **suppliers → users → products → sales → sale_items → inventory_logs → audit_logs → company_settings**,
     capturing `RETURNING id` and building `local_id → supabase id` maps.
  4. users: **drop** `PasswordHash`, `pin`, `QRCode`, `Passkeys` (never ship credentials).
  5. Image columns (`image_url`, `photo_url`, `logo_url`) filled from S3 uploads — see §5.1.
  6. Record run in `sync_log`.
- **No deletes** — mirror the audit trail (`is_active=false`, `is_void=true`); deleted LAN rows just stop updating.

### 5.1 Image upload (S3, implemented in Phase 1.5)
- S3-compatible API on Supabase: endpoint `https://<ref>.storage.supabase.co/storage/v1/s3`, region `us-east-1`, path-style addressing (ForcePathStyle).
- Upload LAN image files → write resulting public URL into `image_url` / `photo_url` / `logo_url`:
  - `Images\users\<file>` → `images/avatar/<file>` (users.photo_url)
  - `Images\products\<file>` → `images/product/<file>` (products.image_url, first image via ProductImages/ProductImageMapping)
  - `Images\company\<file>` → `images/company/<file>` (company_settings.logo_url)
- Uses `AWSSDK.S3` NuGet; keys read from `supabase.config.json` → `s3` section. Missing/failed uploads log a note and sync continues (URL stays NULL).

## 6. POS hooks & button (code touch points)
- **Sync button + status**: `Forms/System/Sys.vb` → `btnSyncCloud` + status label (done).
- **Sale success hook**: `Forms/Sales/Sales.vb` — after the sale transaction commits.
- **Stock change hook**: wherever `InventoryLog` rows are inserted (AddProduct, Inventory IN/OUT/ADJUST).
- Hooks run fire-and-forget on a background thread; never block the UI or transaction.

## 7. Dashboard (Laravel, separate repo `jadeclinic-dashboard`)
- Laravel 12 + Breeze/React; admin login only (Breeze users — separate from POS staff).
- Dual DB: SQLite for auth, `pgsql` **read-only** `dashboard_ro` connection for data.
- Read-only Eloquent models for the 8 data tables (no migrations for Supabase tables).
- Pages: Overview (KPIs + charts), Products (`image_url` thumbs, expiry/stock alerts), Sales (receipt view via `sales_data`), Inventory, Suppliers.
- Images render directly from public `image_url`; placeholder fallback.

## 8. AI service (FastAPI, later phase)
- Predict/analyze from real Supabase data (forecasting, anomalies, summaries).
- **Runtime schema introspection** — no schema file needed: query `information_schema.columns` + table row counts + low-cardinality sampled values, inject into every LLM prompt.
- **No hallucination:** deterministic stats computed in code; LLM only paraphrases real numbers; structured output with validation (Pydantic).
- Uses the same `dashboard_ro` connection as Laravel.

## 9. Security & secrets
| Side | Storage | Notes |
|---|---|---|
| **LAN sync (POS)** | `%LocalAppData%\JadeClinic\supabase.config.json` (git-ignored, outside repo) | DSN + S3 keys. Env var `JADECLINIC_SUPABASE_DSN` overrides. Commit `supabase.config.example.json` with placeholders only |
| **Laravel web** | `.env` (gitignored) | `DB_*` Supabase + Breeze auth |
| **FastAPI AI** | `.env` (gitignored) + `.env.example` | `DATABASE_URL` |

- `service_role`/S3 keys = full write access → never in repo, never on the web side.
- **Hardening (Phase 2):** encrypt the LAN config via **Windows Credential Manager / DPAPI** so keys aren't plaintext at rest.
- Dashboard admin is a separate Breeze user; POS credentials never leave the LAN.

## 10. Phases
| Phase | Deliverable | Depends on |
|---|---|---|
| **1. Central DB** | POS reads configured DB path; staff connect over LAN; LAN SQLite spike | — |
| **1.5 Sync button** | `SupabaseSync.vb` full 8-table sync behind System Settings button (done) | Supabase project + schema applied |
| **2. JadeSync full** | Scheduled worker + sale/stock hooks + image uploads (S3) + `sync_log` UI | Phase 1.5 |
| **3. Dashboard** | Laravel + Breeze + read-only pages | Phase 2 data |
| **4. AI** | FastAPI insights with runtime introspection | Phase 3 |

## 11. Risks & mitigations
| Risk | Mitigation |
|---|---|
| SQLite over LAN (SMB locking, corruption) | WAL + busy_timeout; low terminal count; Phase 1 spike on real LAN |
| `service_role`/S3 key leak | Local git-ignored config only; DPAPI hardening; rotate if exposed |
| LAN schema divergence (`Products` expiry variants) | Sync maps columns defensively (column-exists checks) |
| Large syncs | Full upsert trivial at clinic scale; watermark sync later if needed |
| AI hallucination | Runtime schema + real data only, deterministic stats, structured validation |
