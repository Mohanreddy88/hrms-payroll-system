namespace HrmsApi.Models;

public class Attendance
{
    public int      Id         { get; set; }
    public int      EmployeeId { get; set; }
    public DateTime Date       { get; set; }
    public string   Status     { get; set; } = "Present"; // Present, Absent, Leave, HalfDay
    public DateTime? CheckIn   { get; set; }  // Check-in time
    public DateTime? CheckOut  { get; set; }  // Check-out time
    public decimal  WorkHours  { get; set; }  // Total work hours for the day
    public string   Remarks    { get; set; } = string.Empty;
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

    // Navigation
    public Employee Employee { get; set; } = null!;
}
