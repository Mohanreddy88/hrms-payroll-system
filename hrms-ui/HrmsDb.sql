-- ============================================================
-- HRMS Database Setup Script
-- Run in SQL Server Management Studio (SSMS) or Azure Data Studio
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'HrmsDb')
BEGIN
    CREATE DATABASE HrmsDb;
    PRINT 'Database HrmsDb created.';
END
GO

USE HrmsDb;
GO

-- ============================================================
-- TABLE: Users
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        Username     NVARCHAR(100) NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        Role         NVARCHAR(50)  NOT NULL DEFAULT 'Admin',
        IsActive     BIT           NOT NULL DEFAULT 1,
        CreatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_Users_Username UNIQUE (Username)
    );
    PRINT 'Table Users created.';
END
GO

-- ============================================================
-- TABLE: BankMaster
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BankMaster')
BEGIN
    CREATE TABLE BankMaster (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        Name        NVARCHAR(150) NOT NULL,
        IsActive    BIT           NOT NULL DEFAULT 1,
        CreatedDate DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        UpdatedDate DATETIME2     NULL,
        CreatedBy   NVARCHAR(100) NOT NULL DEFAULT 'system',
        UpdatedBy   NVARCHAR(100) NULL,
        CONSTRAINT UQ_BankMaster_Name UNIQUE (Name)
    );
    PRINT 'Table BankMaster created.';
END
GO

-- ============================================================
-- TABLE: Employees
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Employees')
BEGIN
    CREATE TABLE Employees (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        Name          NVARCHAR(150) NOT NULL,
        Email         NVARCHAR(150) NOT NULL,
        Phone         NVARCHAR(30)  NOT NULL,
        Department    NVARCHAR(100) NOT NULL,
        Designation   NVARCHAR(100) NOT NULL,
        JoinDate      DATETIME2     NOT NULL,
        Salary        DECIMAL(18,2) NOT NULL DEFAULT 0,
        IsActive      BIT           NOT NULL DEFAULT 1,
        CreatedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        IcPassport    NVARCHAR(50)  NOT NULL DEFAULT '',
        TaxNumber     NVARCHAR(50)  NOT NULL DEFAULT '',
        BankId        INT           NULL,
        AccountNumber NVARCHAR(30)  NOT NULL DEFAULT '',
        CONSTRAINT UQ_Employees_Email UNIQUE (Email),
        CONSTRAINT FK_Employees_BankMaster FOREIGN KEY (BankId) REFERENCES BankMaster(Id)
    );
    PRINT 'Table Employees created.';
END
GO

-- ============================================================
-- TABLE: Attendances
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Attendances')
BEGIN
    CREATE TABLE Attendances (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeId  INT           NOT NULL,
        Date        DATE          NOT NULL,
        Status      NVARCHAR(20)  NOT NULL DEFAULT 'Present',
        Remarks     NVARCHAR(255) NOT NULL DEFAULT '',
        CreatedAt   DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_Attendances_Employees
            FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE,

        CONSTRAINT UQ_Attendance_Employee_Date
            UNIQUE (EmployeeId, Date)
    );
    PRINT 'Table Attendances created.';
END
GO

-- ============================================================
-- TABLE: Payrolls
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Payrolls')
BEGIN
    CREATE TABLE Payrolls (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeId  INT           NOT NULL,
        Month       INT           NOT NULL,
        Year        INT           NOT NULL,
        BasicSalary DECIMAL(18,2) NOT NULL DEFAULT 0,
        Allowances  DECIMAL(18,2) NOT NULL DEFAULT 0,
        Deductions  DECIMAL(18,2) NOT NULL DEFAULT 0,
        EpfAmount   DECIMAL(18,2) NOT NULL DEFAULT 0,  -- 2% of BasicSalary
        SocsoAmount DECIMAL(18,2) NOT NULL DEFAULT 0,  -- 0.5% of BasicSalary
        TaxAmount   DECIMAL(18,2) NOT NULL DEFAULT 0,  -- 11.97% of GrossIncome (PCB)
        GrossIncome DECIMAL(18,2) NOT NULL DEFAULT 0,  -- BasicSalary + Allowances
        NetSalary   DECIMAL(18,2) NOT NULL DEFAULT 0,  -- GrossIncome - EPF - SOCSO - Tax - Deductions
        GeneratedOn DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_Payrolls_Employees
            FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE,

        CONSTRAINT CHK_Payrolls_Month CHECK (Month BETWEEN 1 AND 12),
        CONSTRAINT CHK_Payrolls_Year  CHECK (Year  BETWEEN 2000 AND 2099),

        CONSTRAINT UQ_Payroll_Employee_Month_Year
            UNIQUE (EmployeeId, Month, Year)
    );
    PRINT 'Table Payrolls created.';
END
GO

-- ============================================================
-- ALTER: Add new statutory columns if table already exists
-- (Run this block if you created the table before this update)
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Payrolls')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Payrolls') AND name = 'EpfAmount')
        ALTER TABLE Payrolls ADD EpfAmount DECIMAL(18,2) NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Payrolls') AND name = 'SocsoAmount')
        ALTER TABLE Payrolls ADD SocsoAmount DECIMAL(18,2) NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Payrolls') AND name = 'TaxAmount')
        ALTER TABLE Payrolls ADD TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Payrolls') AND name = 'GrossIncome')
        ALTER TABLE Payrolls ADD GrossIncome DECIMAL(18,2) NOT NULL DEFAULT 0;

    PRINT 'Payrolls table columns updated (EPF, SOCSO, Tax, GrossIncome added if missing).';
END
GO

-- ============================================================
-- SEED: Admin user
-- Login: admin / admin123
-- BCrypt hash generated with work factor 11
-- Regenerate in production with: BCrypt.Net.BCrypt.HashPassword("yourpassword")
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, PasswordHash, Role)
    VALUES (
        'admin',
        '$2a$11$3/eFaHJGZuWxQAZ1.n4iYuyTMdXPX1l5JoFWUeAz3zIIKlJyvxfIO',
        'Admin'
    );
    PRINT 'Admin user seeded (password: admin123).';
END
GO

-- ============================================================
-- SEED: Sample employees
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Employees WHERE Email = 'john.smith@hrms.com')
BEGIN
    INSERT INTO Employees (Name, Email, Phone, Department, Designation, JoinDate, Salary, IsActive)
    VALUES
        ('John Smith',  'john.smith@hrms.com',  '+60123456789', 'IT',         'Software Engineer',   '2023-01-15', 5000.00, 1),
        ('Sarah Lee',   'sarah.lee@hrms.com',   '+60123456780', 'HR',         'HR Manager',          '2022-06-01', 6500.00, 1),
        ('Michael Tan', 'michael.tan@hrms.com', '+60123456781', 'Finance',    'Accountant',          '2023-03-10', 4800.00, 1),
        ('Priya Kumar', 'priya.kumar@hrms.com', '+60123456782', 'Marketing',  'Marketing Executive', '2023-07-20', 4200.00, 1),
        ('Ali Hassan',  'ali.hassan@hrms.com',  '+60123456783', 'Operations', 'Operations Manager',  '2021-11-01', 7000.00, 1),
        ('Emily Wong',  'emily.wong@hrms.com',  '+60123456784', 'IT',         'UI/UX Designer',      '2024-01-08', 5200.00, 1),
        ('David Rajan', 'david.rajan@hrms.com', '+60123456785', 'Sales',      'Sales Executive',     '2023-09-12', 4500.00, 0);
    PRINT 'Sample employees seeded.';
END
GO

-- ============================================================
-- SEED: Sample attendance for today
-- ============================================================
DECLARE @today DATE = CAST(GETDATE() AS DATE);
DECLARE @emp1  INT  = (SELECT Id FROM Employees WHERE Email = 'john.smith@hrms.com');
DECLARE @emp2  INT  = (SELECT Id FROM Employees WHERE Email = 'sarah.lee@hrms.com');

IF @emp1 IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM Attendances WHERE EmployeeId = @emp1 AND Date = @today)
BEGIN
    INSERT INTO Attendances (EmployeeId, Date, Status, Remarks)
    VALUES (@emp1, @today, 'Present', '');
END

IF @emp2 IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM Attendances WHERE EmployeeId = @emp2 AND Date = @today)
BEGIN
    INSERT INTO Attendances (EmployeeId, Date, Status, Remarks)
    VALUES (@emp2, @today, 'Present', 'On time');
END
GO

-- ============================================================
-- Verify row counts
-- ============================================================
SELECT 'Users'        AS [Table], COUNT(*) AS [Rows] FROM Users
UNION ALL
SELECT 'Employees',   COUNT(*) FROM Employees
UNION ALL
SELECT 'Attendances', COUNT(*) FROM Attendances
UNION ALL
SELECT 'Payrolls',    COUNT(*) FROM Payrolls;
GO

PRINT 'HrmsDb setup complete!';
GO
