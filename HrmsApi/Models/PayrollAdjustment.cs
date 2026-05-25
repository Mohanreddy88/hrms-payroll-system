namespace HrmsApi.Models;

public class PayrollAdjustment
{
    public int Id { get; set; }
    public int PayrollId { get; set; }
    public string AdjustmentType { get; set; } = string.Empty; // Allowance, Deduction, Bonus, Overtime
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Payroll Payroll { get; set; } = null!;
    public User Creator { get; set; } = null!;
}
