namespace HrmsApi.Models;

public class AuditLog
{
    public int     Id          { get; set; }
    public int?    UserId      { get; set; }
    public string  Username    { get; set; } = string.Empty;
    public string  Action      { get; set; } = string.Empty; // Created, Updated, Deleted, Approved, Rejected, etc.
    public string  EntityType  { get; set; } = string.Empty; // Employee, Payroll, LeaveRequest, AttendancePeriod, etc.
    public string? EntityId    { get; set; }
    public string  Description { get; set; } = string.Empty;
    public string? OldValues   { get; set; } // JSON snapshot before change
    public string? NewValues   { get; set; } // JSON snapshot after change
    public string? IpAddress   { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
}
