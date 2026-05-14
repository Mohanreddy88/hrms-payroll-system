namespace HrmsApi.Models;

/// <summary>
/// Core employee entity — maps to the Employees table.
/// </summary>
public class Employee
{
    public int      Id            { get; set; }
    public string   EmployeeCode  { get; set; } = string.Empty;
    public string   Name          { get; set; } = string.Empty;
    public string   Email         { get; set; } = string.Empty;
    public string   Phone         { get; set; } = string.Empty;
    
    /// <summary>Foreign key to Department — employee's department</summary>
    public int?     DepartmentId  { get; set; }
    
    public string   Designation   { get; set; } = string.Empty;
    public DateTime JoinDate      { get; set; }
    public decimal  Salary        { get; set; }
    public bool     IsActive      { get; set; } = true;
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;
    
    /// <summary>Profile picture URL or path</summary>
    public string   ProfilePicture { get; set; } = string.Empty;

    // ── New fields (Bank & statutory identity) ──────────────
    /// <summary>IC or Passport number (used on payslip as NRIC NO)</summary>
    public string   IcPassport    { get; set; } = string.Empty;

    /// <summary>Tax Identification Number for PCB/Income Tax purposes</summary>
    public string   TaxNumber     { get; set; } = string.Empty;

    /// <summary>Foreign key to BankMaster — employee's salary bank</summary>
    public int?     BankId        { get; set; }

    /// <summary>Bank account number for salary disbursement</summary>
    public string   AccountNumber { get; set; } = string.Empty;

    // ── Navigation properties ────────────────────────────────
    public Department?              Department  { get; set; }
    public BankMaster?              Bank        { get; set; }
    public ICollection<Attendance>  Attendances { get; set; } = new List<Attendance>();
    public ICollection<Payroll>     Payrolls    { get; set; } = new List<Payroll>();
}
