namespace HrmsApi.Models;

public class Payroll
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }

    // Statutory deductions (auto-calculated)
    public decimal EpfAmount { get; set; }    // 2% employee contribution
    public decimal SocsoAmount { get; set; }  // 0.5% of BasicSalary
    public decimal TaxAmount { get; set; }    // 11.97% of GrossIncome (PCB)

    public decimal GrossIncome { get; set; }
    public decimal NetSalary { get; set; }
    public DateTime GeneratedOn { get; set; } = DateTime.UtcNow;

    // Enhanced tracking fields
    public string Status { get; set; } = "Draft"; // Draft, Under Review, Pending Approval, Approved, Processed, Rejected
    public decimal? AttendanceHours { get; set; }
    public decimal? ExpectedHours { get; set; }
    public int? PaidLeaveDays { get; set; }
    public int? UnpaidLeaveDays { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public int? ProcessedBy { get; set; }
    public DateTime? ProcessedOn { get; set; }
    public string? Remarks { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public User? Approver { get; set; }
    public User? Processor { get; set; }
    public ICollection<PayrollAttendancePeriod> AttendancePeriods { get; set; } = new List<PayrollAttendancePeriod>();
    public ICollection<PayrollLeaveRequest> LeaveRequests { get; set; } = new List<PayrollLeaveRequest>();
    public ICollection<PayrollAdjustment> Adjustments { get; set; } = new List<PayrollAdjustment>();
}
