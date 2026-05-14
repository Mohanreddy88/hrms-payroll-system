using HrmsApi.Models;

namespace HrmsApi.Services;

public interface IPayrollCalculationService
{
    /// <summary>
    /// Check if an employee is eligible for payroll generation for a given month/year
    /// </summary>
    Task<PayrollEligibilityResult> CheckEligibilityAsync(int employeeId, int month, int year);

    /// <summary>
    /// Calculate payroll for a single employee for a given month/year
    /// </summary>
    Task<PayrollCalculationResult> CalculatePayrollAsync(int employeeId, int month, int year);

    /// <summary>
    /// Generate payroll record and save to database
    /// </summary>
    Task<Payroll> GeneratePayrollAsync(int employeeId, int month, int year, int createdById);

    /// <summary>
    /// Generate payroll for multiple employees
    /// </summary>
    Task<BulkPayrollGenerationResult> GenerateBulkPayrollAsync(List<int> employeeIds, int month, int year, int createdById);

    /// <summary>
    /// Approve payroll record
    /// </summary>
    Task<Payroll> ApprovePayrollAsync(int payrollId, int approvedById, string? remarks = null);

    /// <summary>
    /// Reject payroll record
    /// </summary>
    Task<Payroll> RejectPayrollAsync(int payrollId, int rejectedById, string reason);

    /// <summary>
    /// Mark payroll as processed (payment completed)
    /// </summary>
    Task<Payroll> ProcessPayrollAsync(int payrollId, int processedById);
}

public class PayrollEligibilityResult
{
    public bool IsEligible { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int TotalAttendancePeriods { get; set; }
    public int ApprovedAttendancePeriods { get; set; }
    public int PendingAttendancePeriods { get; set; }
    public int TotalLeaveRequests { get; set; }
    public int PendingLeaveRequests { get; set; }
}

public class PayrollCalculationResult
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal ExpectedHours { get; set; }
    public decimal AttendanceHours { get; set; }
    public int WorkingDays { get; set; }
    public int PaidLeaveDays { get; set; }
    public int UnpaidLeaveDays { get; set; }
    public decimal UnpaidLeaveDeduction { get; set; }
    public decimal Allowances { get; set; }
    public decimal ManualDeductions { get; set; }
    public decimal GrossIncome { get; set; }
    public decimal EpfAmount { get; set; }
    public decimal SocsoAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetSalary { get; set; }
    public List<AttendancePeriodSummary> AttendancePeriods { get; set; } = new();
    public List<LeaveRequestSummary> LeaveRequests { get; set; } = new();
}

public class AttendancePeriodSummary
{
    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal HoursWorked { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class LeaveRequestSummary
{
    public int Id { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public string LeaveTypeCode { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public bool IsPaid { get; set; }
    public decimal DeductionAmount { get; set; }
}

public class BulkPayrollGenerationResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<PayrollGenerationError> Errors { get; set; } = new();
    public List<int> GeneratedPayrollIds { get; set; } = new();
}

public class PayrollGenerationError
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
