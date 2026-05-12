namespace HrmsApi.Models;

/// <summary>
/// Leave type master data (e.g., Annual Leave, Medical Leave)
/// </summary>
public class LeaveType
{
    public int      Id                  { get; set; }
    public string   Name                { get; set; } = string.Empty;
    public string   Code                { get; set; } = string.Empty;
    public string?  Description         { get; set; }
    public int      DefaultDaysPerYear  { get; set; } = 14;
    public bool     IsActive            { get; set; } = true;
    public bool     RequiresApproval    { get; set; } = true;
    public bool     IsPaid              { get; set; } = true;
    public DateTime CreatedAt           { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<EmployeeLeaveBalance> LeaveBalances { get; set; } = new List<EmployeeLeaveBalance>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}

/// <summary>
/// Employee leave balance per year
/// </summary>
public class EmployeeLeaveBalance
{
    public int      Id                  { get; set; }
    public int      EmployeeId          { get; set; }
    public int      LeaveTypeId         { get; set; }
    public int      Year                { get; set; }
    public decimal  TotalDays           { get; set; }
    public decimal  UsedDays            { get; set; }
    public decimal  BalanceDays         { get; set; }
    public decimal  CarryForwardDays    { get; set; }
    public DateTime CreatedAt           { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt           { get; set; } = DateTime.UtcNow;

    // Navigation
    public Employee  Employee  { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
}

/// <summary>
/// Leave request (application for leave)
/// </summary>
public class LeaveRequest
{
    public int       Id                  { get; set; }
    public int       EmployeeId          { get; set; }
    public int       LeaveTypeId         { get; set; }
    public DateTime  StartDate           { get; set; }
    public DateTime  EndDate             { get; set; }
    public decimal   TotalDays           { get; set; }
    public string?   Reason              { get; set; }
    public string    Status              { get; set; } = "Pending";  // Pending, Approved, Rejected, Cancelled
    public DateTime  RequestedOn         { get; set; } = DateTime.UtcNow;
    public int?      ApprovedBy          { get; set; }
    public DateTime? ApprovedOn          { get; set; }
    public string?   ApprovalRemarks     { get; set; }
    public DateTime? CancelledOn         { get; set; }
    public string?   CancellationReason  { get; set; }

    // Navigation
    public Employee  Employee  { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
}

// ══════════════════════════════════════════════════════════════════════════════
// Request/Response DTOs
// ══════════════════════════════════════════════════════════════════════════════

public class LeaveTypeRequest
{
    public string Name                { get; set; } = string.Empty;
    public string Code                { get; set; } = string.Empty;
    public string? Description        { get; set; }
    public int    DefaultDaysPerYear  { get; set; } = 14;
    public bool   IsActive            { get; set; } = true;
    public bool   RequiresApproval    { get; set; } = true;
    public bool   IsPaid              { get; set; } = true;
}

public class LeaveRequestCreate
{
    public int       EmployeeId  { get; set; }
    public int       LeaveTypeId { get; set; }
    public DateTime  StartDate   { get; set; }
    public DateTime  EndDate     { get; set; }
    public string?   Reason      { get; set; }
}

public class LeaveRequestApproval
{
    public string  Status           { get; set; } = "Approved";  // Approved or Rejected
    public string? ApprovalRemarks  { get; set; }
}

public class LeaveBalanceInit
{
    public int EmployeeId  { get; set; }
    public int LeaveTypeId { get; set; }
    public int Year        { get; set; }
    public decimal TotalDays { get; set; }
}
