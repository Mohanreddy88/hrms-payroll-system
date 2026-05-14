-- =====================================================
-- PAYROLL SYSTEM ENHANCEMENT MIGRATION
-- Approach 1: Attendance-Driven Automated Payroll
-- =====================================================

-- 1. Enhance Payrolls table with detailed tracking
ALTER TABLE Payrolls ADD Status NVARCHAR(50) NOT NULL DEFAULT 'Draft';
ALTER TABLE Payrolls ADD AttendanceHours DECIMAL(10,2) NULL;
ALTER TABLE Payrolls ADD ExpectedHours DECIMAL(10,2) NULL;
ALTER TABLE Payrolls ADD PaidLeaveDays INT NULL;
ALTER TABLE Payrolls ADD UnpaidLeaveDays INT NULL;
ALTER TABLE Payrolls ADD ApprovedBy INT NULL;
ALTER TABLE Payrolls ADD ApprovedOn DATETIME2 NULL;
ALTER TABLE Payrolls ADD ProcessedBy INT NULL;
ALTER TABLE Payrolls ADD ProcessedOn DATETIME2 NULL;
ALTER TABLE Payrolls ADD Remarks NVARCHAR(MAX) NULL;

-- Add foreign keys for approver and processor
ALTER TABLE Payrolls ADD CONSTRAINT FK_Payrolls_ApprovedBy_Users FOREIGN KEY (ApprovedBy) REFERENCES Users(Id);
ALTER TABLE Payrolls ADD CONSTRAINT FK_Payrolls_ProcessedBy_Users FOREIGN KEY (ProcessedBy) REFERENCES Users(Id);

-- 2. Create PayrollAttendancePeriods linking table
CREATE TABLE PayrollAttendancePeriods (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PayrollId INT NOT NULL,
    AttendancePeriodId INT NOT NULL,
    HoursWorked DECIMAL(10,2) NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_PayrollAttendancePeriods_Payroll FOREIGN KEY (PayrollId) REFERENCES Payrolls(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PayrollAttendancePeriods_AttendancePeriod FOREIGN KEY (AttendancePeriodId) REFERENCES AttendancePeriods(Id),
    CONSTRAINT UQ_PayrollAttendancePeriod UNIQUE (PayrollId, AttendancePeriodId)
);

-- 3. Create PayrollLeaveRequests linking table
CREATE TABLE PayrollLeaveRequests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PayrollId INT NOT NULL,
    LeaveRequestId INT NOT NULL,
    LeaveDays DECIMAL(5,2) NOT NULL,
    IsPaid BIT NOT NULL DEFAULT 1,
    DeductionAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_PayrollLeaveRequests_Payroll FOREIGN KEY (PayrollId) REFERENCES Payrolls(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PayrollLeaveRequests_LeaveRequest FOREIGN KEY (LeaveRequestId) REFERENCES LeaveRequests(Id),
    CONSTRAINT UQ_PayrollLeaveRequest UNIQUE (PayrollId, LeaveRequestId)
);

-- 4. Create PayrollAdjustments for manual adjustments
CREATE TABLE PayrollAdjustments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PayrollId INT NOT NULL,
    AdjustmentType NVARCHAR(50) NOT NULL, -- 'Allowance', 'Deduction', 'Bonus', 'Overtime'
    Description NVARCHAR(500) NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_PayrollAdjustments_Payroll FOREIGN KEY (PayrollId) REFERENCES Payrolls(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PayrollAdjustments_CreatedBy_Users FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
);

-- 5. Add indexes for performance
CREATE INDEX IX_Payrolls_Status ON Payrolls(Status);
CREATE INDEX IX_Payrolls_EmployeeId_Month_Year ON Payrolls(EmployeeId, Month, Year);
CREATE INDEX IX_PayrollAttendancePeriods_PayrollId ON PayrollAttendancePeriods(PayrollId);
CREATE INDEX IX_PayrollLeaveRequests_PayrollId ON PayrollLeaveRequests(PayrollId);
CREATE INDEX IX_PayrollAdjustments_PayrollId ON PayrollAdjustments(PayrollId);

-- 6. Create unique constraint to prevent duplicate payrolls
ALTER TABLE Payrolls ADD CONSTRAINT UQ_Payroll_Employee_Month_Year UNIQUE (EmployeeId, Month, Year);

PRINT 'Payroll enhancement migration completed successfully!';
