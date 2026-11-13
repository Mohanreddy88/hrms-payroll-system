-- ============================================================
-- HRMS Railway PostgreSQL Migration Script
-- Run this manually in Railway database query panel
-- Changes since initial deployment:
--   1. AuditLogs table (new)
--   2. MustChangePassword column on Users (new)
-- ============================================================

-- ── 1. AuditLogs table ──────────────────────────────────────
CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id"          SERIAL PRIMARY KEY,
    "UserId"      INTEGER NULL,
    "Username"    VARCHAR(100) NOT NULL,
    "Action"      VARCHAR(100) NOT NULL,
    "EntityType"  VARCHAR(100) NOT NULL,
    "EntityId"    VARCHAR(50)  NULL,
    "Description" VARCHAR(1000) NOT NULL,
    "OldValues"   TEXT NULL,
    "NewValues"   TEXT NULL,
    "IpAddress"   VARCHAR(50)  NULL,
    "CreatedAt"   TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_EntityType_EntityId"
    ON "AuditLogs" ("EntityType", "EntityId");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_UserId"
    ON "AuditLogs" ("UserId");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CreatedAt"
    ON "AuditLogs" ("CreatedAt" DESC);

-- ── 2. MustChangePassword column on Users ───────────────────
ALTER TABLE "Users"
    ADD COLUMN IF NOT EXISTS "MustChangePassword" BOOLEAN NOT NULL DEFAULT FALSE;

-- ── Verify ──────────────────────────────────────────────────
SELECT 'AuditLogs table' AS check_item,
       COUNT(*) AS column_count
FROM information_schema.columns
WHERE table_name = 'AuditLogs';

SELECT 'MustChangePassword column' AS check_item,
       COUNT(*) AS exists_count
FROM information_schema.columns
WHERE table_name = 'Users'
  AND column_name = 'MustChangePassword';
