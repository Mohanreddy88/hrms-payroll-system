namespace HrmsApi.Models;

/// <summary>
/// Represents a bank in the BankMaster reference table.
/// Used as a foreign key from Employees.BankId.
/// </summary>
public class BankMaster
{
    public int      Id          { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public bool     IsActive    { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public string   CreatedBy   { get; set; } = "system";
    public string?  UpdatedBy   { get; set; }

    // Navigation — employees who use this bank
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
