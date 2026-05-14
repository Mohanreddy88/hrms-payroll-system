-- ================================================
-- HRMS PostgreSQL Database Migration Script
-- ================================================
-- This script creates the complete database schema for PostgreSQL
-- Run this script on Railway PostgreSQL database after deployment
-- ================================================

-- Create Departments table
CREATE TABLE IF NOT EXISTS "Departments" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "IsActive" BOOLEAN NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL
);

-- Create BankMaster table
CREATE TABLE IF NOT EXISTS "BankMaster" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "IsActive" BOOLEAN NOT NULL,
    "CreatedDate" TIMESTAMP NOT NULL,
    "UpdatedDate" TIMESTAMP NULL,
    "CreatedBy" TEXT NOT NULL,
    "UpdatedBy" TEXT NULL
);

-- Create LeaveTypes table
CREATE TABLE IF NOT EXISTS "LeaveTypes" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Code" TEXT NOT NULL,
    "Description" TEXT NULL,
    "DefaultDaysPerYear" INTEGER NOT NULL,
    "IsActive" BOOLEAN NOT NULL,
    "RequiresApproval" BOOLEAN NOT NULL,
    "IsPaid" BOOLEAN NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL
);

-- Create PublicHolidays table
CREATE TABLE IF NOT EXISTS "PublicHolidays" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Date" TIMESTAMP NOT NULL,
    "Year" INTEGER NOT NULL,
    "IsNational" BOOLEAN NOT NULL,
    "State" TEXT NULL,
    "Description" TEXT NULL,
    "CreatedAt" TIMESTAMP NOT NULL
);

-- Create Users table
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" SERIAL PRIMARY KEY,
    "Username" TEXT NOT NULL,
    "Email" VARCHAR(255) NULL,
    "PasswordHash" TEXT NOT NULL,
    "Role" TEXT NOT NULL,
    "IsActive" BOOLEAN NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL
);

-- Create unique filtered index on Email (excluding NULL values)
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" 
ON "Users" ("Email") 
WHERE "Email" IS NOT NULL;

-- Create Employees table
CREATE TABLE IF NOT EXISTS "Employees" (
    "Id" SERIAL PRIMARY KEY,
    "EmployeeCode" VARCHAR(20) NULL,
    "Name" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "Phone" TEXT NOT NULL,
    "DepartmentId" INTEGER NULL,
    "Designation" TEXT NOT NULL,
    "JoinDate" TIMESTAMP NOT NULL,
    "Salary" NUMERIC(18,2) NOT NULL,
    "IsActive" BOOLEAN NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "ProfilePicture" TEXT NOT NULL DEFAULT '',
    "IcPassport" TEXT NOT NULL DEFAULT '',
    "TaxNumber" TEXT NOT NULL DEFAULT '',
    "BankId" INTEGER NULL,
    "AccountNumber" TEXT NOT NULL DEFAULT '',
    "DateOfBirth" TIMESTAMP NULL,
    "Gender" TEXT NULL,
    "Address" TEXT NULL,
    "City" TEXT NULL,
    "State" TEXT NULL,
    "PostalCode" TEXT NULL,
    "Country" TEXT NULL,
    "EmergencyContactName" TEXT NULL,
    "EmergencyContactPhone" TEXT NULL,
    "EmergencyContactRelation" TEXT NULL,
    "EmploymentType" TEXT NULL,
    "ContractEndDate" TIMESTAMP NULL,
    "ProbationEndDate" TIMESTAMP NULL,
    "ResignationDate" TIMESTAMP NULL,
    "LastWorkingDate" TIMESTAMP NULL,
    "TerminationDate" TIMESTAMP NULL,
    "TerminationReason" TEXT NULL,
    "EpfNumber" TEXT NULL,
    "SocsoNumber" TEXT NULL,
    "EisNumber" TEXT NULL,
    "TaxFileNumber" TEXT NULL,
    CONSTRAINT "FK_Employees_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") 
        REFERENCES "Departments" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Employees_BankMaster_BankId" FOREIGN KEY ("BankId") 
        REFERENCES "BankMaster" ("Id") ON DELETE SET NULL
);

-- Create unique index on EmployeeCode
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Employees_EmployeeCode" 
ON "Employees" ("EmployeeCode") 
WHERE "EmployeeCode" IS NOT NULL;

-- Create indexes on Employees
CREATE INDEX IF NOT EXISTS "IX_Employees_DepartmentId" ON "Employees" ("DepartmentId");
CREATE INDEX IF NOT EXISTS "IX_Employees_BankId" ON "Employees" ("BankId");

-- Create AttendancePeriods table
CREATE TABLE IF NOT EXISTS "AttendancePeriods" (
    "Id" SERIAL PRIMARY KEY,
    "EmployeeId" INTEGER NOT NULL,
    "StartDate" TIMESTAMP NOT NULL,
    "EndDate" TIMESTAMP NOT NULL,
    "Status" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "SubmittedAt" TIMESTAMP NULL,
    "ApprovedAt" TIMESTAMP NULL,
    "RejectedAt" TIMESTAMP NULL,
    "ApprovedBy" INTEGER NULL,
    "RejectedBy" INTEGER NULL,
    "RejectionReason" TEXT NULL,
    "Remarks" TEXT NULL,
    CONSTRAINT "FK_AttendancePeriods_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") 
        REFERENCES "Employees" ("Id") ON DELETE CASCADE
);

-- Create unique index on AttendancePeriods
CREATE UNIQUE INDEX IF NOT EXISTS "IX_AttendancePeriods_EmployeeId_StartDate_EndDate" 
ON "AttendancePeriods" ("EmployeeId", "StartDate", "EndDate");

-- Create AttendancePeriodDays table
CREATE TABLE IF NOT EXISTS "AttendancePeriodDays" (
    "Id" SERIAL PRIMARY KEY,
    "AttendancePeriodId" INTEGER NOT NULL,
    "Date" TIMESTAMP NOT NULL,
    "Hours" NUMERIC(5,2) NOT NULL,
    "Note" TEXT NULL,
    "Remarks" TEXT NULL,
    "IsPublicHoliday" BOOLEAN NOT NULL,
    "IsWeekend" BOOLEAN NOT NULL,
    CONSTRAINT "FK_AttendancePeriodDays_AttendancePeriods_AttendancePeriodId" FOREIGN KEY ("AttendancePeriodId") 
        REFERENCES "AttendancePeriods" ("Id") ON DELETE CASCADE
);

-- Create unique index on AttendancePeriodDays
CREATE UNIQUE INDEX IF NOT EXISTS "IX_AttendancePeriodDays_AttendancePeriodId_Date" 
ON "AttendancePeriodDays" ("AttendancePeriodId", "Date");

-- Create Attendances table (legacy)
CREATE TABLE IF NOT EXISTS "Attendances" (
    "Id" SERIAL PRIMARY KEY,
    "EmployeeId" INTEGER NOT NULL,
    "Date" TIMESTAMP NOT NULL,
    "Status" TEXT NOT NULL,
    "CheckIn" TIMESTAMP NULL,
    "CheckOut" TIMESTAMP NULL,
    "WorkHours" NUMERIC(10,2) NOT NULL,
    "Remarks" TEXT NOT NULL DEFAULT '',
    "CreatedAt" TIMESTAMP NOT NULL,
    CONSTRAINT "FK_Attendances_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") 
        REFERENCES "Employees" ("Id") ON DELETE CASCADE
);

-- Create unique index on Attendances
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Attendances_EmployeeId_Date" 
ON "Attendances" ("EmployeeId", "Date");

-- Create EmployeeLeaveBalances table
CREATE TABLE IF NOT EXISTS "EmployeeLeaveBalances" (
    "Id" SERIAL PRIMARY KEY,
    "EmployeeId" INTEGER NOT NULL,
    "LeaveTypeId" INTEGER NOT NULL,
    "Year" INTEGER NOT NULL,
    "TotalDays" NUMERIC(5,2) NOT NULL,
    "UsedDays" NUMERIC(5,2) NOT NULL,
    "BalanceDays" NUMERIC(5,2) NOT NULL,
    "CarryForwardDays" NUMERIC(5,2) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP NOT NULL,
    CONSTRAINT "FK_EmployeeLeaveBalances_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") 
        REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EmployeeLeaveBalances_LeaveTypes_LeaveTypeId" FOREIGN KEY ("LeaveTypeId") 
        REFERENCES "LeaveTypes" ("Id") ON DELETE CASCADE
);

-- Create unique index on EmployeeLeaveBalances
CREATE UNIQUE INDEX IF NOT EXISTS "IX_EmployeeLeaveBalances_EmployeeId_LeaveTypeId_Year" 
ON "EmployeeLeaveBalances" ("EmployeeId", "LeaveTypeId", "Year");

CREATE INDEX IF NOT EXISTS "IX_EmployeeLeaveBalances_LeaveTypeId" 
ON "EmployeeLeaveBalances" ("LeaveTypeId");

-- Create LeaveRequests table
CREATE TABLE IF NOT EXISTS "LeaveRequests" (
    "Id" SERIAL PRIMARY KEY,
    "EmployeeId" INTEGER NOT NULL,
    "LeaveTypeId" INTEGER NOT NULL,
    "StartDate" TIMESTAMP NOT NULL,
    "EndDate" TIMESTAMP NOT NULL,
    "TotalDays" NUMERIC(5,2) NOT NULL,
    "Reason" TEXT NULL,
    "Status" TEXT NOT NULL,
    "RequestedOn" TIMESTAMP NOT NULL,
    "ApprovedBy" INTEGER NULL,
    "ApprovedOn" TIMESTAMP NULL,
    "ApprovalRemarks" TEXT NULL,
    "CancelledOn" TIMESTAMP NULL,
    "CancellationReason" TEXT NULL,
    CONSTRAINT "FK_LeaveRequests_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") 
        REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LeaveRequests_LeaveTypes_LeaveTypeId" FOREIGN KEY ("LeaveTypeId") 
        REFERENCES "LeaveTypes" ("Id") ON DELETE RESTRICT
);

-- Create indexes on LeaveRequests
CREATE INDEX IF NOT EXISTS "IX_LeaveRequests_EmployeeId" ON "LeaveRequests" ("EmployeeId");
CREATE INDEX IF NOT EXISTS "IX_LeaveRequests_LeaveTypeId" ON "LeaveRequests" ("LeaveTypeId");

-- Create Payrolls table
CREATE TABLE IF NOT EXISTS "Payrolls" (
    "Id" SERIAL PRIMARY KEY,
    "EmployeeId" INTEGER NOT NULL,
    "Month" INTEGER NOT NULL,
    "Year" INTEGER NOT NULL,
    "BasicSalary" NUMERIC(18,2) NOT NULL,
    "Allowances" NUMERIC(18,2) NOT NULL,
    "Deductions" NUMERIC(18,2) NOT NULL,
    "EpfAmount" NUMERIC(18,2) NOT NULL,
    "SocsoAmount" NUMERIC(18,2) NOT NULL,
    "TaxAmount" NUMERIC(18,2) NOT NULL,
    "GrossIncome" NUMERIC(18,2) NOT NULL,
    "NetSalary" NUMERIC(18,2) NOT NULL,
    "GeneratedOn" TIMESTAMP NOT NULL,
    "Status" TEXT NULL,
    "PaidOn" TIMESTAMP NULL,
    "PaymentMethod" TEXT NULL,
    "PaymentReference" TEXT NULL,
    "Remarks" TEXT NULL,
    CONSTRAINT "FK_Payrolls_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") 
        REFERENCES "Employees" ("Id") ON DELETE CASCADE
);

-- Create unique index on Payrolls
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Payrolls_EmployeeId_Month_Year" 
ON "Payrolls" ("EmployeeId", "Month", "Year");

-- Create PayrollAdjustments table
CREATE TABLE IF NOT EXISTS "PayrollAdjustments" (
    "Id" SERIAL PRIMARY KEY,
    "PayrollId" INTEGER NOT NULL,
    "Type" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Amount" NUMERIC(18,2) NOT NULL,
    "IsRecurring" BOOLEAN NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    CONSTRAINT "FK_PayrollAdjustments_Payrolls_PayrollId" FOREIGN KEY ("PayrollId") 
        REFERENCES "Payrolls" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_PayrollAdjustments_PayrollId" 
ON "PayrollAdjustments" ("PayrollId");

-- Create PayrollAttendancePeriods table
CREATE TABLE IF NOT EXISTS "PayrollAttendancePeriods" (
    "Id" SERIAL PRIMARY KEY,
    "PayrollId" INTEGER NOT NULL,
    "AttendancePeriodId" INTEGER NOT NULL,
    CONSTRAINT "FK_PayrollAttendancePeriods_Payrolls_PayrollId" FOREIGN KEY ("PayrollId") 
        REFERENCES "Payrolls" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PayrollAttendancePeriods_AttendancePeriods_AttendancePeriodId" FOREIGN KEY ("AttendancePeriodId") 
        REFERENCES "AttendancePeriods" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_PayrollAttendancePeriods_PayrollId" 
ON "PayrollAttendancePeriods" ("PayrollId");

CREATE INDEX IF NOT EXISTS "IX_PayrollAttendancePeriods_AttendancePeriodId" 
ON "PayrollAttendancePeriods" ("AttendancePeriodId");

-- Create PayrollLeaveRequests table
CREATE TABLE IF NOT EXISTS "PayrollLeaveRequests" (
    "Id" SERIAL PRIMARY KEY,
    "PayrollId" INTEGER NOT NULL,
    "LeaveRequestId" INTEGER NOT NULL,
    CONSTRAINT "FK_PayrollLeaveRequests_Payrolls_PayrollId" FOREIGN KEY ("PayrollId") 
        REFERENCES "Payrolls" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PayrollLeaveRequests_LeaveRequests_LeaveRequestId" FOREIGN KEY ("LeaveRequestId") 
        REFERENCES "LeaveRequests" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_PayrollLeaveRequests_PayrollId" 
ON "PayrollLeaveRequests" ("PayrollId");

CREATE INDEX IF NOT EXISTS "IX_PayrollLeaveRequests_LeaveRequestId" 
ON "PayrollLeaveRequests" ("LeaveRequestId");

-- Create Timesheets table
CREATE TABLE IF NOT EXISTS "Timesheets" (
    "Id" SERIAL PRIMARY KEY,
    "EmployeeId" INTEGER NOT NULL,
    "Month" INTEGER NOT NULL,
    "Year" INTEGER NOT NULL,
    "TotalWorkingDays" INTEGER NOT NULL,
    "TotalPresent" INTEGER NOT NULL,
    "TotalMedicalLeave" INTEGER NOT NULL,
    "TotalAbsent" INTEGER NOT NULL,
    "TotalLeave" INTEGER NOT NULL,
    "TotalHalfDay" INTEGER NOT NULL,
    "TotalPublicHolidays" INTEGER NOT NULL,
    "TotalWorkHours" NUMERIC(10,2) NOT NULL,
    "GeneratedOn" TIMESTAMP NOT NULL,
    "Status" TEXT NOT NULL,
    "ApprovedBy" INTEGER NULL,
    "ApprovedOn" TIMESTAMP NULL,
    "Remarks" TEXT NULL,
    CONSTRAINT "FK_Timesheets_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") 
        REFERENCES "Employees" ("Id") ON DELETE CASCADE
);

-- Create unique index on Timesheets
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Timesheets_EmployeeId_Year_Month" 
ON "Timesheets" ("EmployeeId", "Year", "Month");

-- Create Notifications table
CREATE TABLE IF NOT EXISTS "Notifications" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Type" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "Message" TEXT NOT NULL,
    "IsRead" BOOLEAN NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "ReadAt" TIMESTAMP NULL,
    "RelatedEntityType" TEXT NULL,
    "RelatedEntityId" INTEGER NULL
);

CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId_IsRead" 
ON "Notifications" ("UserId", "IsRead");

-- Create __EFMigrationsHistory table for EF Core
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" VARCHAR(150) PRIMARY KEY,
    "ProductVersion" VARCHAR(32) NOT NULL
);

-- Insert migration history record
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260512173034_InitialCreate', '8.0.0')
ON CONFLICT DO NOTHING;

-- ================================================
-- SEED DATA
-- ================================================

-- Insert Leave Types
INSERT INTO "LeaveTypes" ("Name", "Code", "Description", "DefaultDaysPerYear", "IsActive", "RequiresApproval", "IsPaid", "CreatedAt")
VALUES 
    ('Annual Leave', 'AL', 'Standard annual leave', 14, true, true, true, NOW()),
    ('Medical Leave', 'ML', 'Medical/sick leave with certificate', 14, true, true, true, NOW()),
    ('Emergency Leave', 'EL', 'Emergency or compassionate leave', 2, true, true, true, NOW()),
    ('Casual Leave', 'CL', 'Short-term casual leave', 3, true, true, true, NOW()),
    ('Maternity Leave', 'MTL', 'Maternity leave for female employees', 98, true, true, true, NOW()),
    ('Paternity Leave', 'PTL', 'Paternity leave for male employees', 7, true, true, true, NOW()),
    ('Unpaid Leave', 'UL', 'Leave without pay', 0, true, true, false, NOW()),
    ('Study Leave', 'SL', 'Educational or training leave', 5, true, true, true, NOW()),
    ('Hajj Leave', 'HL', 'Religious pilgrimage leave', 0, true, true, false, NOW()),
    ('Replacement Leave', 'RL', 'Replacement for overtime or holidays', 0, true, true, true, NOW())
ON CONFLICT DO NOTHING;

-- ================================================
-- COMPLETION MESSAGE
-- ================================================
DO $$
BEGIN
    RAISE NOTICE '✅ HRMS PostgreSQL Migration Completed Successfully!';
    RAISE NOTICE '   - All tables created';
    RAISE NOTICE '   - Indexes and constraints applied';
    RAISE NOTICE '   - Leave types seeded (10 types)';
    RAISE NOTICE '';
    RAISE NOTICE '📋 Next Steps:';
    RAISE NOTICE '   1. Create admin user via API or manually';
    RAISE NOTICE '   2. Add employees through the admin interface';
    RAISE NOTICE '   3. Initialize leave balances for all employees';
END $$;
