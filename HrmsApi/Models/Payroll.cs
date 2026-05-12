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
    public decimal EpfAmount { get; set; }    // 2% of BasicSalary
    public decimal SocsoAmount { get; set; }  // 0.5% of BasicSalary
    public decimal TaxAmount { get; set; }    // 11.97% of GrossIncome

    public decimal GrossIncome { get; set; }
    public decimal NetSalary { get; set; }
    public DateTime GeneratedOn { get; set; } = DateTime.UtcNow;

    // Navigation
    public Employee Employee { get; set; } = null!;
}
