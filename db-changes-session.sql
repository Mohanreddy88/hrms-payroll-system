-- ═══════════════════════════════════════════════════════════════
-- DB changes — run manually on Railway PostgreSQL
-- ═══════════════════════════════════════════════════════════════

-- 1. Password reset columns on Users table
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PasswordResetToken" TEXT NULL;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PasswordResetTokenExpiry" TIMESTAMP WITH TIME ZONE NULL;

-- 2. DailyRate column on Payrolls table (payroll calculation improvement)
ALTER TABLE "Payrolls" ADD COLUMN IF NOT EXISTS "DailyRate" NUMERIC(18,2) NOT NULL DEFAULT 0;

-- 3. LeaveBalance UpdatedAt column
ALTER TABLE "EmployeeLeaveBalances" ADD COLUMN IF NOT EXISTS "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL;
