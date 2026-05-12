-- ════════════════════════════════════════════════════════════════════════════
-- Add Time Tracking Columns to Attendance Table
-- Created: 2026-05-10
-- Description: Adds CheckIn, CheckOut, and WorkHours columns to track time
-- ════════════════════════════════════════════════════════════════════════════

USE HrmsDb;
GO

-- Check if columns already exist before adding
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Attendances') AND name = 'CheckIn')
BEGIN
    ALTER TABLE Attendances ADD CheckIn DATETIME NULL;
    PRINT '✅ Added CheckIn column to Attendances table';
END
ELSE
BEGIN
    PRINT 'ℹ️ CheckIn column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Attendances') AND name = 'CheckOut')
BEGIN
    ALTER TABLE Attendances ADD CheckOut DATETIME NULL;
    PRINT '✅ Added CheckOut column to Attendances table';
END
ELSE
BEGIN
    PRINT 'ℹ️ CheckOut column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Attendances') AND name = 'WorkHours')
BEGIN
    ALTER TABLE Attendances ADD WorkHours DECIMAL(10,2) NOT NULL DEFAULT 0;
    PRINT '✅ Added WorkHours column to Attendances table';
END
ELSE
BEGIN
    PRINT 'ℹ️ WorkHours column already exists';
END
GO

-- Update existing attendance records to calculate work hours if CheckIn and CheckOut exist
UPDATE Attendances
SET WorkHours = DATEDIFF(MINUTE, CheckIn, CheckOut) / 60.0
WHERE CheckIn IS NOT NULL 
  AND CheckOut IS NOT NULL 
  AND CheckOut > CheckIn
  AND WorkHours = 0;
GO

-- For Present records without CheckIn/CheckOut, set default 8 hours
UPDATE Attendances
SET WorkHours = 8.0
WHERE Status = 'Present' 
  AND WorkHours = 0
  AND (CheckIn IS NULL OR CheckOut IS NULL);
GO

PRINT '✅ Attendance table updated with time tracking columns';
PRINT 'ℹ️ Existing records updated with default work hours where applicable';
GO
