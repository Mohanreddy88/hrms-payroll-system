-- Migration: Add Department table and update Employee table (SQL Server)
-- Run this script on your SQL Server database

-- 1. Create Departments table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Departments')
BEGIN
    CREATE TABLE [Departments] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(MAX) NOT NULL DEFAULT '',
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- 2. Add new columns to Employees table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'DepartmentId')
BEGIN
    ALTER TABLE [Employees] 
    ADD [DepartmentId] INT NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'ProfilePicture')
BEGIN
    ALTER TABLE [Employees] 
    ADD [ProfilePicture] NVARCHAR(500) NOT NULL DEFAULT '';
END
GO

-- 3. Create foreign key constraint
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Employees_Departments_DepartmentId')
BEGIN
    ALTER TABLE [Employees]
    ADD CONSTRAINT [FK_Employees_Departments_DepartmentId] 
    FOREIGN KEY ([DepartmentId]) 
    REFERENCES [Departments]([Id]) 
    ON DELETE SET NULL;
END
GO

-- 4. Create index on DepartmentId for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Employees_DepartmentId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Employees_DepartmentId] 
    ON [Employees]([DepartmentId]);
END
GO

-- 5. Insert sample departments
IF NOT EXISTS (SELECT * FROM [Departments] WHERE [Name] = 'Engineering')
BEGIN
    INSERT INTO [Departments] ([Name], [Description], [IsActive], [CreatedAt])
    VALUES 
        ('Engineering', 'Software Development and IT Infrastructure', 1, GETUTCDATE()),
        ('Human Resources', 'HR Management and Employee Relations', 1, GETUTCDATE()),
        ('Finance', 'Accounting and Financial Management', 1, GETUTCDATE()),
        ('Sales & Marketing', 'Sales Operations and Marketing Campaigns', 1, GETUTCDATE()),
        ('Operations', 'Business Operations and Process Management', 1, GETUTCDATE());
END
GO

-- 6. Verification queries
PRINT 'Departments table created: ' + CAST(CASE WHEN EXISTS(SELECT * FROM sys.tables WHERE name = 'Departments') THEN 1 ELSE 0 END AS NVARCHAR(1));
PRINT 'DepartmentId column added: ' + CAST(CASE WHEN EXISTS(SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'DepartmentId') THEN 1 ELSE 0 END AS NVARCHAR(1));
PRINT 'ProfilePicture column added: ' + CAST(CASE WHEN EXISTS(SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'ProfilePicture') THEN 1 ELSE 0 END AS NVARCHAR(1));

-- View inserted departments
SELECT * FROM [Departments];
GO
