-- Drop all existing tables and let EF Core migrations recreate them properly
-- This ensures the schema matches the C# models exactly

-- Drop tables in correct order (respecting foreign keys)
DROP TABLE IF EXISTS "AttendancePeriodDays" CASCADE;
DROP TABLE IF EXISTS "Attendances" CASCADE;
DROP TABLE IF EXISTS "EmployeeLeaveBalances" CASCADE;
DROP TABLE IF EXISTS "LeaveRequests" CASCADE;
DROP TABLE IF EXISTS "PayrollAdjustments" CASCADE;
DROP TABLE IF EXISTS "PayrollAttendancePeriods" CASCADE;
DROP TABLE IF EXISTS "PayrollLeaveRequests" CASCADE;
DROP TABLE IF EXISTS "Payrolls" CASCADE;
DROP TABLE IF EXISTS "Timesheets" CASCADE;
DROP TABLE IF EXISTS "AttendancePeriods" CASCADE;
DROP TABLE IF EXISTS "Employees" CASCADE;
DROP TABLE IF EXISTS "Notifications" CASCADE;
DROP TABLE IF EXISTS "Users" CASCADE;
DROP TABLE IF EXISTS "PublicHolidays" CASCADE;
DROP TABLE IF EXISTS "LeaveTypes" CASCADE;
DROP TABLE IF EXISTS "BankMaster" CASCADE;
DROP TABLE IF EXISTS "Departments" CASCADE;
DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;

-- Now tables are clean - Railway will recreate them on next deployment using EF Core migrations
SELECT 'All tables dropped - ready for EF Core to recreate them!' as status;
