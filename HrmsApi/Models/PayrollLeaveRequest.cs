namespace HrmsApi.Models;

public class PayrollLeaveRequest
{
    public int Id { get; set; }
    public int PayrollId { get; set; }
    public int LeaveRequestId { get; set; }
    public decimal LeaveDays { get; set; }
    public bool IsPaid { get; set; } = true;
    public decimal DeductionAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Payroll Payroll { get; set; } = null!;
    public LeaveRequest LeaveRequest { get; set; } = null!;
}
