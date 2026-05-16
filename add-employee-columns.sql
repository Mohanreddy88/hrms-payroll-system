-- =====================================================
-- Add Missing Columns to Employees Table
-- =====================================================
-- Run this script in Railway PostgreSQL Database

-- Add AccountNumber column
ALTER TABLE "Employees" 
ADD COLUMN IF NOT EXISTS "AccountNumber" VARCHAR(50) DEFAULT '' NOT NULL;

-- Add IcPassport column (if not exists)
ALTER TABLE "Employees" 
ADD COLUMN IF NOT EXISTS "IcPassport" VARCHAR(50) DEFAULT '' NOT NULL;

-- Add TaxNumber column (if not exists)
ALTER TABLE "Employees" 
ADD COLUMN IF NOT EXISTS "TaxNumber" VARCHAR(50) DEFAULT '' NOT NULL;

-- Add BankId column (if not exists)
ALTER TABLE "Employees" 
ADD COLUMN IF NOT EXISTS "BankId" INTEGER NULL;

-- Add foreign key constraint for BankId (if not exists)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_Employees_BankMaster_BankId'
    ) THEN
        ALTER TABLE "Employees" 
        ADD CONSTRAINT "FK_Employees_BankMaster_BankId" 
        FOREIGN KEY ("BankId") REFERENCES "BankMaster"("Id") ON DELETE SET NULL;
    END IF;
END $$;

-- Verify the columns were added
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'Employees'
  AND column_name IN ('AccountNumber', 'IcPassport', 'TaxNumber', 'BankId')
ORDER BY column_name;
