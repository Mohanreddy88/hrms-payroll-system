namespace HrmsApi.Models;

public class PayrollAttendancePeriod
{
    public int Id { get; set; }
    public int PayrollId { get; set; }
    public int AttendancePeriodId { get; set; }
    public decimal HoursWorked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Payroll Payroll { get; set; } = null!;
    public AttendancePeriod AttendancePeriod { get; set; } = null!;
}
