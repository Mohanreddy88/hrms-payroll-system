namespace HrmsApi.Models;

/// <summary>
/// Monthly timesheet for an employee
/// </summary>
public class Timesheet
{
    public int      Id                  { get; set; }
    public int      EmployeeId          { get; set; }
    public int      Month               { get; set; }  // 1-12
    public int      Year                { get; set; }
    public int      TotalWorkingDays    { get; set; }
    public int      TotalPresent        { get; set; }  // Regular working days with hours filled
    public int      TotalMedicalLeave   { get; set; }  // MC count
    public int      TotalAbsent         { get; set; }  // AL count (Annual Leave)
    public int      TotalLeave          { get; set; }  // EL count (Emergency Leave)
    public int      TotalHalfDay        { get; set; }
    public int      TotalPublicHolidays { get; set; }
    public decimal  TotalWorkHours      { get; set; }
    public DateTime GeneratedOn         { get; set; } = DateTime.UtcNow;
    public string   Status              { get; set; } = "Draft";  // Draft, Submitted, Approved, Rejected
    public int?     ApprovedBy          { get; set; }
    public DateTime? ApprovedOn         { get; set; }
    public string?  Remarks             { get; set; }

    // Navigation
    public Employee Employee { get; set; } = null!;
}

public class TimesheetRequest
{
    public int  EmployeeId { get; set; }
    public int  Month      { get; set; }
    public int  Year       { get; set; }
    public string? Remarks { get; set; }
}
