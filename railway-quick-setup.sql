-- Quick Railway Setup - Copy and paste this ENTIRE script and run it
-- This creates all tables in a single transaction

DROP TABLE IF EXISTS test_simple;
DROP TABLE IF EXISTS "PayrollAdjustments" CASCADE;
DROP TABLE IF EXISTS "PayrollLeaveRequests" CASCADE;
DROP TABLE IF EXISTS "PayrollAttendancePeriods" CASCADE;
DROP TABLE IF EXISTS "Payrolls" CASCADE;
DROP TABLE IF EXISTS "AttendancePeriodDays" CASCADE;
DROP TABLE IF EXISTS "AttendancePeriods" CASCADE;
DROP TABLE IF EXISTS "Attendances" CASCADE;
DROP TABLE IF EXISTS "Timesheets" CASCADE;
DROP TABLE IF EXISTS "LeaveRequests" CASCADE;
DROP TABLE IF EXISTS "EmployeeLeaveBalances" CASCADE;
DROP TABLE IF EXISTS "Notifications" CASCADE;
DROP TABLE IF EXISTS "Employees" CASCADE;
DROP TABLE IF EXISTS "Users" CASCADE;
DROP TABLE IF EXISTS "LeaveTypes" CASCADE;
DROP TABLE IF EXISTS "PublicHolidays" CASCADE;
DROP TABLE IF EXISTS "Departments" CASCADE;
DROP TABLE IF EXISTS "BankMaster" CASCADE;

CREATE TABLE "BankMaster" ("Id" serial PRIMARY KEY, "Name" text NOT NULL, "IsActive" boolean NOT NULL, "CreatedDate" timestamptz NOT NULL, "UpdatedDate" timestamptz, "CreatedBy" text NOT NULL, "UpdatedBy" text);
CREATE TABLE "Departments" ("Id" serial PRIMARY KEY, "Name" text NOT NULL, "Description" text NOT NULL, "IsActive" boolean NOT NULL, "CreatedAt" timestamptz NOT NULL);
CREATE TABLE "LeaveTypes" ("Id" serial PRIMARY KEY, "Name" text NOT NULL, "Code" text NOT NULL, "Description" text, "DefaultDaysPerYear" int NOT NULL, "IsActive" boolean NOT NULL, "RequiresApproval" boolean NOT NULL, "IsPaid" boolean NOT NULL, "CreatedAt" timestamptz NOT NULL);
CREATE TABLE "PublicHolidays" ("Id" serial PRIMARY KEY, "Name" text NOT NULL, "Date" timestamptz NOT NULL, "Year" int NOT NULL, "IsNational" boolean NOT NULL, "State" text, "Description" text, "CreatedAt" timestamptz NOT NULL);
CREATE TABLE "Users" ("Id" serial PRIMARY KEY, "Username" text NOT NULL, "Email" varchar(255) NOT NULL, "PasswordHash" text NOT NULL, "Role" text NOT NULL, "IsActive" boolean NOT NULL, "CreatedAt" timestamptz NOT NULL);
CREATE TABLE "Employees" ("Id" serial PRIMARY KEY, "EmployeeCode" varchar(20) NOT NULL, "FirstName" text NOT NULL, "LastName" text NOT NULL, "Email" text NOT NULL, "PhoneNumber" text, "DateOfBirth" timestamptz, "Gender" text, "Address" text, "City" text, "State" text, "PostalCode" text, "Country" text, "DepartmentId" int REFERENCES "Departments"("Id") ON DELETE SET NULL, "Designation" text, "JoiningDate" timestamptz NOT NULL, "EmploymentType" text, "Salary" numeric(18,2) NOT NULL, "BankId" int REFERENCES "BankMaster"("Id") ON DELETE SET NULL, "BankAccountNumber" text, "PanNumber" text, "AadharNumber" text, "PfNumber" text, "EsiNumber" text, "IsActive" boolean NOT NULL, "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz);
CREATE TABLE "Attendances" ("Id" serial PRIMARY KEY, "EmployeeId" int NOT NULL REFERENCES "Employees"("Id") ON DELETE CASCADE, "Date" timestamptz NOT NULL, "TimeIn" timestamptz, "TimeOut" timestamptz, "WorkHours" numeric(10,2) NOT NULL, "Status" text NOT NULL, "Remarks" text, "CreatedAt" timestamptz NOT NULL);
CREATE TABLE "AttendancePeriods" ("Id" serial PRIMARY KEY, "EmployeeId" int NOT NULL REFERENCES "Employees"("Id") ON DELETE CASCADE, "StartDate" timestamptz NOT NULL, "EndDate" timestamptz NOT NULL, "TotalDays" int NOT NULL, "WorkingDays" int NOT NULL, "PresentDays" int NOT NULL, "AbsentDays" int NOT NULL, "LeaveDays" int NOT NULL, "HolidayDays" int NOT NULL, "TotalHours" int NOT NULL, "CreatedAt" timestamptz NOT NULL);
CREATE TABLE "AttendancePeriodDays" ("Id" serial PRIMARY KEY, "AttendancePeriodId" int NOT NULL REFERENCES "AttendancePeriods"("Id") ON DELETE CASCADE, "Date" timestamptz NOT NULL, "DayType" text NOT NULL, "Hours" numeric(5,2) NOT NULL, "Remarks" text);
CREATE TABLE "EmployeeLeaveBalances" ("Id" serial PRIMARY KEY, "EmployeeId" int NOT NULL REFERENCES "Employees"("Id") ON DELETE CASCADE, "LeaveTypeId" int NOT NULL REFERENCES "LeaveTypes"("Id") ON DELETE CASCADE, "Year" int NOT NULL, "TotalDays" numeric(5,2) NOT NULL, "UsedDays" numeric(5,2) NOT NULL, "BalanceDays" numeric(5,2) NOT NULL, "CarryForwardDays" numeric(5,2) NOT NULL, "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz);
CREATE TABLE "LeaveRequests" ("Id" serial PRIMARY KEY, "EmployeeId" int NOT NULL REFERENCES "Employees"("Id") ON DELETE CASCADE, "LeaveTypeId" int NOT NULL REFERENCES "LeaveTypes"("Id") ON DELETE RESTRICT, "StartDate" timestamptz NOT NULL, "EndDate" timestamptz NOT NULL, "TotalDays" numeric(5,2) NOT NULL, "Reason" text NOT NULL, "Status" text NOT NULL, "ApprovedBy" int, "ApprovedDate" timestamptz, "ApproverRemarks" text, "CreatedAt" timestamptz NOT NULL);
CREATE TABLE "Notifications" ("Id" serial PRIMARY KEY, "UserId" int NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE, "Title" text NOT NULL, "Message" text NOT NULL, "Type" text NOT NULL, "IsRead" boolean NOT NULL, "CreatedAt" timestamptz NOT NULL, "ReadAt" timestamptz);
CREATE TABLE "Timesheets" ("Id" serial PRIMARY KEY, "EmployeeId" int NOT NULL REFERENCES "Employees"("Id") ON DELETE CASCADE, "Year" int NOT NULL, "Month" int NOT NULL, "TotalWorkHours" numeric(10,2) NOT NULL, "Status" text NOT NULL, "SubmittedDate" timestamptz, "ApprovedDate" timestamptz, "ApprovedBy" int, "CreatedAt" timestamptz NOT NULL);
CREATE TABLE "Payrolls" ("Id" serial PRIMARY KEY, "EmployeeId" int NOT NULL REFERENCES "Employees"("Id") ON DELETE CASCADE, "Month" int NOT NULL, "Year" int NOT NULL, "BasicSalary" numeric(18,2) NOT NULL, "Allowances" numeric(18,2) NOT NULL, "Deductions" numeric(18,2) NOT NULL, "EpfAmount" numeric(18,2) NOT NULL, "SocsoAmount" numeric(18,2) NOT NULL, "TaxAmount" numeric(18,2) NOT NULL, "GrossIncome" numeric(18,2) NOT NULL, "NetSalary" numeric(18,2) NOT NULL, "Status" text NOT NULL, "ProcessedDate" timestamptz, "ProcessedBy" int REFERENCES "Users"("Id") ON DELETE RESTRICT, "ApprovedDate" timestamptz, "ApprovedBy" int REFERENCES "Users"("Id") ON DELETE RESTRICT, "PaidDate" timestamptz, "PaymentMethod" text, "TransactionId" text, "Remarks" text, "AttendanceHours" numeric(10,2) NOT NULL, "ExpectedHours" numeric(10,2) NOT NULL, "CreatedAt" timestamptz NOT NULL);
CREATE TABLE "PayrollAdjustments" ("Id" serial PRIMARY KEY, "PayrollId" int NOT NULL REFERENCES "Payrolls"("Id") ON DELETE CASCADE, "Type" text NOT NULL, "Description" text NOT NULL, "Amount" numeric(10,2) NOT NULL, "CreatedBy" int NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT, "CreatedAt" timestamptz NOT NULL);
CREATE TABLE "PayrollAttendancePeriods" ("Id" serial PRIMARY KEY, "PayrollId" int NOT NULL REFERENCES "Payrolls"("Id") ON DELETE CASCADE, "AttendancePeriodId" int NOT NULL REFERENCES "AttendancePeriods"("Id") ON DELETE RESTRICT, "HoursWorked" numeric(10,2) NOT NULL);
CREATE TABLE "PayrollLeaveRequests" ("Id" serial PRIMARY KEY, "PayrollId" int NOT NULL REFERENCES "Payrolls"("Id") ON DELETE CASCADE, "LeaveRequestId" int NOT NULL REFERENCES "LeaveRequests"("Id") ON DELETE RESTRICT, "LeaveDays" numeric(5,2) NOT NULL, "DeductionAmount" numeric(10,2) NOT NULL);

CREATE UNIQUE INDEX "IX_Employees_EmployeeCode" ON "Employees"("EmployeeCode");
CREATE UNIQUE INDEX "IX_Attendances_EmployeeId_Date" ON "Attendances"("EmployeeId", "Date");
CREATE UNIQUE INDEX "IX_Payrolls_EmployeeId_Month_Year" ON "Payrolls"("EmployeeId", "Month", "Year");
CREATE UNIQUE INDEX "IX_Users_Email" ON "Users"("Email") WHERE "Email" IS NOT NULL AND "Email" <> '';

INSERT INTO "LeaveTypes" ("Name", "Code", "Description", "DefaultDaysPerYear", "IsActive", "RequiresApproval", "IsPaid", "CreatedAt") VALUES 
('Annual Leave', 'AL', 'Annual paid leave', 14, true, true, true, NOW()),
('Medical Leave', 'ML', 'Medical/Sick leave', 14, true, true, true, NOW()),
('Emergency Leave', 'EL', 'Emergency leave', 5, true, true, true, NOW()),
('Unpaid Leave', 'UL', 'Unpaid leave', 0, true, true, false, NOW()),
('Maternity Leave', 'MAT', 'Maternity leave', 60, true, true, true, NOW()),
('Paternity Leave', 'PAT', 'Paternity leave', 7, true, true, true, NOW());

INSERT INTO "Users" ("Username", "Email", "PasswordHash", "Role", "IsActive", "CreatedAt") VALUES ('admin', 'admin@hrms.local', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'Admin', true, NOW());

SELECT COUNT(*) as total_tables FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE';
