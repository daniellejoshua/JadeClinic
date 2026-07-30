-- SQLite Migration: File-based images (ImageData BLOB -> FilePath TEXT)
-- Run this ONCE on the existing jadeclinic.db before enabling file-based storage.

-- ============================================================
-- Add sync tracking columns to existing tables
-- ============================================================

ALTER TABLE Users ADD COLUMN synced INTEGER DEFAULT 0;
ALTER TABLE Users ADD COLUMN synced_at DATETIME;

ALTER TABLE Products ADD COLUMN synced INTEGER DEFAULT 0;
ALTER TABLE Products ADD COLUMN synced_at DATETIME;
ALTER TABLE Products ADD COLUMN UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP;

ALTER TABLE Suppliers ADD COLUMN synced INTEGER DEFAULT 0;
ALTER TABLE Suppliers ADD COLUMN synced_at DATETIME;
ALTER TABLE Suppliers ADD COLUMN UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP;

ALTER TABLE Sales ADD COLUMN synced INTEGER DEFAULT 0;
ALTER TABLE Sales ADD COLUMN synced_at DATETIME;
ALTER TABLE Sales ADD COLUMN UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP;

ALTER TABLE SaleItems ADD COLUMN synced INTEGER DEFAULT 0;
ALTER TABLE SaleItems ADD COLUMN synced_at DATETIME;
ALTER TABLE SaleItems ADD COLUMN UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP;

ALTER TABLE InventoryLog ADD COLUMN synced INTEGER DEFAULT 0;
ALTER TABLE InventoryLog ADD COLUMN synced_at DATETIME;

ALTER TABLE AuditLog ADD COLUMN synced INTEGER DEFAULT 0;
ALTER TABLE AuditLog ADD COLUMN synced_at DATETIME;

ALTER TABLE CompanySettings ADD COLUMN synced INTEGER DEFAULT 0;
ALTER TABLE CompanySettings ADD COLUMN synced_at DATETIME;

-- ProductImages no longer needs sync (LAN-only, file-based)

-- ============================================================
-- Add ImagePath column to ProductImages (replaces ImageData BLOB)
-- ============================================================
ALTER TABLE ProductImages ADD COLUMN FilePath TEXT DEFAULT '';

-- ============================================================
-- Replace Photo BLOB with PhotoPath TEXT in Users
-- ============================================================
ALTER TABLE Users ADD COLUMN PhotoPath TEXT NULL;

-- ============================================================
-- Replace Logo BLOB with LogoPath TEXT in CompanySettings
-- ============================================================
ALTER TABLE CompanySettings ADD COLUMN LogoPath TEXT NULL;

-- ============================================================
-- Migration: Extract existing BLOBs to files
-- ============================================================
-- This must be run by a separate script (migration_tool.py or similar)
-- that reads each BLOB, writes to Images/ subfolder, and sets the path.

-- ============================================================
-- New table: AllowedDevices (LAN-only, NOT synced)
-- ============================================================
CREATE TABLE IF NOT EXISTS AllowedDevices (
    DeviceID     INTEGER PRIMARY KEY AUTOINCREMENT,
    DeviceName   TEXT NOT NULL UNIQUE,
    Notes        TEXT,
    IsApproved   INTEGER DEFAULT 0,
    CreatedAt    DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- Ensure WAL mode for better concurrent access
-- ============================================================
PRAGMA journal_mode=WAL;
PRAGMA busy_timeout=5000;
