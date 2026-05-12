-- =====================================================
-- Timesheet & Leave Management System - Database Migration
-- =====================================================
-- Creates tables for:
--   1. PublicHolidays (Malaysia holidays)
--   2. Timesheets (Monthly employee timesheets)
--   3. LeaveTypes (Leave type master data)
--   4. EmployeeLeaveBalances (Per-employee leave balances)
--   5. LeaveRequests (Leave request workflow)
-- =====================================================

USE HrmsDb;
GO

-- =====================================================
-- 1. CREATE TABLE: PublicHolidays
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PublicHolidays')
BEGIN
    CREATE TABLE PublicHolidays (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Date DATE NOT NULL,
        Year INT NOT NULL,
        IsNational BIT NOT NULL DEFAULT 1,
        State NVARCHAR(200) NULL,
        Description NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Table PublicHolidays created successfully.';
END
ELSE
    PRINT 'Table PublicHolidays already exists.';
GO

-- =====================================================
-- 2. CREATE TABLE: Timesheets
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Timesheets')
BEGIN
    CREATE TABLE Timesheets (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeId INT NOT NULL,
        Month INT NOT NULL CHECK (Month BETWEEN 1 AND 12),
        Year INT NOT NULL,
        TotalWorkingDays INT NOT NULL DEFAULT 0,
        TotalPresent INT NOT NULL DEFAULT 0,
        TotalAbsent INT NOT NULL DEFAULT 0,
        TotalLeave INT NOT NULL DEFAULT 0,
        TotalHalfDay INT NOT NULL DEFAULT 0,
        TotalPublicHolidays INT NOT NULL DEFAULT 0,
        TotalWorkHours DECIMAL(10,2) NOT NULL DEFAULT 0,
        GeneratedOn DATETIME NOT NULL DEFAULT GETDATE(),
        Status NVARCHAR(50) NOT NULL DEFAULT 'Draft',
        ApprovedBy INT NULL,
        ApprovedOn DATETIME NULL,
        Remarks NVARCHAR(500) NULL,
        CONSTRAINT FK_Timesheets_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
        CONSTRAINT FK_Timesheets_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES Users(Id)
    );
    PRINT 'Table Timesheets created successfully.';
END
ELSE
    PRINT 'Table Timesheets already exists.';
GO

-- =====================================================
-- 3. CREATE TABLE: LeaveTypes
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LeaveTypes')
BEGIN
    CREATE TABLE LeaveTypes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Code NVARCHAR(20) NOT NULL UNIQUE,
        Description NVARCHAR(500) NULL,
        DefaultDaysPerYear INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        RequiresApproval BIT NOT NULL DEFAULT 1,
        IsPaid BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Table LeaveTypes created successfully.';
END
ELSE
    PRINT 'Table LeaveTypes already exists.';
GO

-- =====================================================
-- 4. CREATE TABLE: EmployeeLeaveBalances
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmployeeLeaveBalances')
BEGIN
    CREATE TABLE EmployeeLeaveBalances (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeId INT NOT NULL,
        LeaveTypeId INT NOT NULL,
        Year INT NOT NULL,
        TotalDays INT NOT NULL DEFAULT 0,
        UsedDays INT NOT NULL DEFAULT 0,
        BalanceDays INT NOT NULL DEFAULT 0,
        CarryForwardDays INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_EmployeeLeaveBalances_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
        CONSTRAINT FK_EmployeeLeaveBalances_LeaveTypes FOREIGN KEY (LeaveTypeId) REFERENCES LeaveTypes(Id)
    );
    PRINT 'Table EmployeeLeaveBalances created successfully.';
END
ELSE
    PRINT 'Table EmployeeLeaveBalances already exists.';
GO

-- =====================================================
-- 5. CREATE TABLE: LeaveRequests
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LeaveRequests')
BEGIN
    CREATE TABLE LeaveRequests (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeId INT NOT NULL,
        LeaveTypeId INT NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        TotalDays INT NOT NULL,
        Reason NVARCHAR(1000) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        RequestedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ApprovedBy INT NULL,
        ApprovedOn DATETIME NULL,
        ApprovalRemarks NVARCHAR(1000) NULL,
        CancelledOn DATETIME NULL,
        CancellationReason NVARCHAR(1000) NULL,
        CONSTRAINT FK_LeaveRequests_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
        CONSTRAINT FK_LeaveRequests_LeaveTypes FOREIGN KEY (LeaveTypeId) REFERENCES LeaveTypes(Id),
        CONSTRAINT FK_LeaveRequests_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES Users(Id)
    );
    PRINT 'Table LeaveRequests created successfully.';
END
ELSE
    PRINT 'Table LeaveRequests already exists.';
GO

-- =====================================================
-- SEED DATA: LeaveTypes (Malaysia Employment Act Standards)
-- =====================================================
IF NOT EXISTS (SELECT * FROM LeaveTypes WHERE Code = 'AL')
BEGIN
    INSERT INTO LeaveTypes (Name, Code, Description, DefaultDaysPerYear, IsActive, RequiresApproval, IsPaid) VALUES
    ('Annual Leave', 'AL', 'Yearly annual leave entitlement', 14, 1, 1, 1),
    ('Medical Leave', 'ML', 'Medical/sick leave with certificate', 14, 1, 1, 1),
    ('Emergency Leave', 'EL', 'Emergency or urgent personal matters', 2, 1, 1, 1),
    ('Compassionate Leave', 'CL', 'Death of immediate family member', 3, 1, 1, 1),
    ('Maternity Leave', 'MTL', 'Maternity leave for female employees', 98, 1, 1, 1),
    ('Paternity Leave', 'PTL', 'Paternity leave for male employees', 7, 1, 1, 1),
    ('Unpaid Leave', 'UL', 'Leave without pay', 0, 1, 1, 0),
    ('Study Leave', 'SL', 'Professional development and training', 5, 1, 1, 1),
    ('Hajj Leave', 'HL', 'Pilgrimage leave for Muslim employees', 0, 1, 1, 0),
    ('Replacement Leave', 'RL', 'Replacement for worked public holidays', 0, 1, 1, 1);
    
    PRINT 'Seeded 10 leave types successfully.';
END
ELSE
    PRINT 'Leave types already seeded.';
GO

-- =====================================================
-- SEED DATA: PublicHolidays - Malaysia 2026
-- =====================================================
IF NOT EXISTS (SELECT * FROM PublicHolidays WHERE Year = 2026)
BEGIN
    INSERT INTO PublicHolidays (Name, Date, Year, IsNational, State, Description) VALUES
    -- National Holidays
    ('New Year''s Day', '2026-01-01', 2026, 1, NULL, 'New Year celebration'),
    ('Federal Territory Day', '2026-02-01', 2026, 0, 'Federal Territories', 'FT Day celebration'),
    ('Chinese New Year', '2026-02-17', 2026, 1, NULL, 'First day of Chinese New Year'),
    ('Chinese New Year (Day 2)', '2026-02-18', 2026, 1, NULL, 'Second day of Chinese New Year'),
    ('Hari Raya Puasa', '2026-04-03', 2026, 1, NULL, 'Eid al-Fitr (Tentative)'),
    ('Hari Raya Puasa (Day 2)', '2026-04-04', 2026, 1, NULL, 'Eid al-Fitr Day 2 (Tentative)'),
    ('Labour Day', '2026-05-01', 2026, 1, NULL, 'International Workers Day'),
    ('Wesak Day', '2026-05-13', 2026, 1, NULL, 'Buddha''s Birthday'),
    ('Agong''s Birthday', '2026-06-06', 2026, 1, NULL, 'King''s Birthday'),
    ('Hari Raya Haji', '2026-06-10', 2026, 1, NULL, 'Eid al-Adha (Tentative)'),
    ('Awal Muharram', '2026-06-30', 2026, 1, NULL, 'Islamic New Year (Tentative)'),
    ('Merdeka Day', '2026-08-31', 2026, 1, NULL, 'Independence Day'),
    ('Prophet Muhammad''s Birthday', '2026-09-09', 2026, 1, NULL, 'Maulidur Rasul (Tentative)'),
    ('Malaysia Day', '2026-09-16', 2026, 1, NULL, 'Formation of Malaysia'),
    ('Deepavali', '2026-10-29', 2026, 1, NULL, 'Festival of Lights'),
    ('Christmas Day', '2026-12-25', 2026, 1, NULL, 'Christian celebration'),
    
    -- State-specific holidays (examples)
    ('Thaipusam', '2026-02-03', 2026, 0, 'Selangor,Federal Territories,Penang', 'Hindu festival'),
    ('Nuzul Al-Quran', '2026-03-23', 2026, 0, 'Selangor,Federal Territories', 'Revelation of Quran (Tentative)');
    
    PRINT 'Seeded Malaysia public holidays for 2026.';
END
ELSE
    PRINT 'Public holidays for 2026 already seeded.';
GO

-- =====================================================
-- SEED DATA: PublicHolidays - Malaysia 2027
-- =====================================================
IF NOT EXISTS (SELECT * FROM PublicHolidays WHERE Year = 2027)
BEGIN
    INSERT INTO PublicHolidays (Name, Date, Year, IsNational, State, Description) VALUES
    ('New Year''s Day', '2027-01-01', 2027, 1, NULL, 'New Year celebration'),
    ('Federal Territory Day', '2027-02-01', 2027, 0, 'Federal Territories', 'FT Day celebration'),
    ('Thaipusam', '2027-01-23', 2027, 0, 'Selangor,Federal Territories,Penang', 'Hindu festival'),
    ('Chinese New Year', '2027-02-06', 2027, 1, NULL, 'First day of Chinese New Year'),
    ('Chinese New Year (Day 2)', '2027-02-07', 2027, 1, NULL, 'Second day of Chinese New Year'),
    ('Hari Raya Puasa', '2027-03-24', 2027, 1, NULL, 'Eid al-Fitr (Tentative)'),
    ('Hari Raya Puasa (Day 2)', '2027-03-25', 2027, 1, NULL, 'Eid al-Fitr Day 2 (Tentative)'),
    ('Labour Day', '2027-05-01', 2027, 1, NULL, 'International Workers Day'),
    ('Wesak Day', '2027-05-02', 2027, 1, NULL, 'Buddha''s Birthday'),
    ('Hari Raya Haji', '2027-05-31', 2027, 1, NULL, 'Eid al-Adha (Tentative)'),
    ('Agong''s Birthday', '2027-06-07', 2027, 1, NULL, 'King''s Birthday'),
    ('Awal Muharram', '2027-06-20', 2027, 1, NULL, 'Islamic New Year (Tentative)'),
    ('Merdeka Day', '2027-08-31', 2027, 1, NULL, 'Independence Day'),
    ('Prophet Muhammad''s Birthday', '2027-08-29', 2027, 1, NULL, 'Maulidur Rasul (Tentative)'),
    ('Malaysia Day', '2027-09-16', 2027, 1, NULL, 'Formation of Malaysia'),
    ('Deepavali', '2027-10-18', 2027, 1, NULL, 'Festival of Lights'),
    ('Christmas Day', '2027-12-25', 2027, 1, NULL, 'Christian celebration');
    
    PRINT 'Seeded Malaysia public holidays for 2027.';
END
ELSE
    PRINT 'Public holidays for 2027 already seeded.';
GO

-- =====================================================
-- VERIFICATION
-- =====================================================
PRINT '';
PRINT '==============================================';
PRINT 'Migration Complete - Summary:';
PRINT '==============================================';
SELECT 'PublicHolidays' AS TableName, COUNT(*) AS RecordCount FROM PublicHolidays
UNION ALL
SELECT 'Timesheets', COUNT(*) FROM Timesheets
UNION ALL
SELECT 'LeaveTypes', COUNT(*) FROM LeaveTypes
UNION ALL
SELECT 'EmployeeLeaveBalances', COUNT(*) FROM EmployeeLeaveBalances
UNION ALL
SELECT 'LeaveRequests', COUNT(*) FROM LeaveRequests;
GO
