-- ═══════════════════════════════════════════════════════════════
-- DB Migration: Payroll Proration Columns
-- Run manually on Railway PostgreSQL
-- Date: 2026-05-25
-- ═══════════════════════════════════════════════════════════════
-- These 4 columns support prorated salary for employees who
-- join mid-month. Added to the Payrolls table.

ALTER TABLE "Payrolls" ADD COLUMN IF NOT EXISTS "IsProrated"     BOOLEAN      NOT NULL DEFAULT FALSE;
ALTER TABLE "Payrolls" ADD COLUMN IF NOT EXISTS "ProratedDays"   INTEGER      NULL;
ALTER TABLE "Payrolls" ADD COLUMN IF NOT EXISTS "TotalMonthDays" INTEGER      NULL;
ALTER TABLE "Payrolls" ADD COLUMN IF NOT EXISTS "ProratedSalary" NUMERIC(18,2) NULL;

-- Verify
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'Payrolls'
  AND column_name IN ('IsProrated', 'ProratedDays', 'TotalMonthDays', 'ProratedSalary')
ORDER BY column_name;
