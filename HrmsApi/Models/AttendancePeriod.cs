namespace HrmsApi.Models;

/// <summary>
/// Represents a period for daily attendance entry (e.g., bi-weekly, monthly periods)
/// </summary>
public class AttendancePeriod
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved, Rejected
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public int? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }
    public string? Remarks { get; set; }

    // Navigation
    public Employee Employee { get; set; } = null!;
    public ICollection<AttendancePeriodDay> Days { get; set; } = new List<AttendancePeriodDay>();
}

/// <summary>
/// Represents a single day entry within an attendance period
/// </summary>
public class AttendancePeriodDay
{
    public int Id { get; set; }
    public int AttendancePeriodId { get; set; }
    public DateTime Date { get; set; }
    public decimal Hours { get; set; } // Work hours for the day (0-24)
    public string? Note { get; set; } // AL, EL, MC, or empty
    public string? Remarks { get; set; } // Additional remarks
    public bool IsPublicHoliday { get; set; }
    public bool IsWeekend { get; set; }

    // Navigation
    public AttendancePeriod AttendancePeriod { get; set; } = null!;
}

/// <summary>
/// Request model for creating or updating an attendance period
/// </summary>
public class SaveAttendancePeriodRequest
{
    public int? Id { get; set; } // Null for create, set for update
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<DayEntryRequest> Days { get; set; } = new();
}

public class DayEntryRequest
{
    public DateTime Date { get; set; }
    public decimal Hours { get; set; }
    public string? Note { get; set; }
    public string? Remarks { get; set; }
    public bool IsPublicHoliday { get; set; }
    public bool IsWeekend { get; set; }
}
