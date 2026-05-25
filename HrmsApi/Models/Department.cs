namespace HrmsApi.Models;

/// <summary>
/// Department master — manages organizational departments.
/// </summary>
public class Department
{
    public int      Id          { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public string   Description { get; set; } = string.Empty;
    public bool     IsActive    { get; set; } = true;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    // ── Navigation properties ────────────────────────────────
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
